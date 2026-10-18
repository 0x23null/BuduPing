package com.buduping.app

import com.google.firebase.database.*
import java.util.UUID

data class DesktopState(val online: Boolean = false, val ts: Long = 0)
data class PairFeedback(
    val eventId: String = "", val status: String = "", val replyId: String = "",
    val replyType: String = "hug", val replyTs: Long = 0,
)

class FirebaseRepo {
    private val db get() = database

    fun sendPing(pairId: String, type: String, text: String, from: String, variant: String = "",
                 onResult: (String, Exception?) -> Unit): String {
        val node = db.getReference("pairs").child(pairId).child("inbox").push()
        val key = requireNotNull(node.key)
        node.setValue(mapOf("id" to UUID.randomUUID().toString(), "type" to type,
            "text" to text.take(80), "from" to from.take(24), "variant" to variant,
            "ts" to ServerValue.TIMESTAMP)) { error, _ -> onResult(key, error?.toException()) }
        return key
    }

    fun observe(pairId: String, onPresence: (DesktopState) -> Unit, onFeedback: (PairFeedback) -> Unit,
                onConnected: (Boolean) -> Unit, onClockOffset: (Long) -> Unit,
                onError: () -> Unit): () -> Unit {
        val subscriptions = mutableListOf<Pair<DatabaseReference, ValueEventListener>>()
        fun listen(ref: DatabaseReference, block: (DataSnapshot) -> Unit) {
            val listener = object : ValueEventListener {
                override fun onDataChange(snapshot: DataSnapshot) = block(snapshot)
                override fun onCancelled(error: DatabaseError) = onError()
            }
            ref.addValueEventListener(listener)
            subscriptions += ref to listener
        }
        val pair = db.getReference("pairs").child(pairId)
        listen(pair.child("presence")) { onPresence(DesktopState(
            it.child("online").getValue(Boolean::class.java) ?: false,
            it.child("ts").getValue(Long::class.java) ?: 0)) }
        listen(pair.child("feedback")) {
            onFeedback(PairFeedback(
                eventId = it.child("delivery/eventId").getValue(String::class.java) ?: "",
                status = it.child("delivery/status").getValue(String::class.java) ?: "",
                replyId = it.child("reply/id").getValue(String::class.java) ?: "",
                replyType = it.child("reply/type").getValue(String::class.java) ?: "hug",
                replyTs = it.child("reply/ts").getValue(Long::class.java) ?: 0))
        }
        listen(db.getReference(".info/connected")) { onConnected(it.getValue(Boolean::class.java) == true) }
        listen(db.getReference(".info/serverTimeOffset")) { onClockOffset(it.getValue(Long::class.java) ?: 0) }
        return { subscriptions.forEach { (ref, listener) -> ref.removeEventListener(listener) } }
    }

    companion object {
        // One initialization per process; pending writes survive an Activity/process restart.
        private val database: FirebaseDatabase by lazy {
            FirebaseDatabase.getInstance().apply { setPersistenceEnabled(true) }
        }
    }
}
