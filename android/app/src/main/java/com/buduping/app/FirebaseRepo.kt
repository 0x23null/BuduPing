package com.buduping.app

import com.google.firebase.database.DataSnapshot
import com.google.firebase.database.DatabaseError
import com.google.firebase.database.FirebaseDatabase
import com.google.firebase.database.ServerValue
import com.google.firebase.database.ValueEventListener
import java.util.UUID

/**
 * Thin wrapper around Firebase Realtime Database.
 * RTDB URL comes from google-services.json; the project lives in asia-southeast1.
 */
class FirebaseRepo {

    private val db: FirebaseDatabase by lazy { FirebaseDatabase.getInstance() }

    /**
     * Push one ping event into /pairs/{pairId}/inbox.
     * Fire-and-forget: the write runs off the UI thread, onError only fires on failure.
     */
    fun sendPing(
        pairId: String,
        type: String,
        text: String,
        from: String,
        onError: (Exception) -> Unit = {},
    ) {
        val event = mapOf(
            "id" to UUID.randomUUID().toString(),
            "type" to type,
            "text" to text,
            "from" to from,
            "ts" to ServerValue.TIMESTAMP,
        )
        db.getReference("pairs").child(pairId).child("inbox")
            .push()
            .setValue(event)
            .addOnFailureListener { onError(it) }
    }

    /**
     * Observe /pairs/{pairId}/presence.
     * The desktop app writes {online: true, ts: <epoch ms>} and heartbeats it every ~20s.
     * Online = online flag set AND ts fresh within 60 seconds (desktop running, tray included).
     */
    fun observePresence(pairId: String, onChange: (online: Boolean) -> Unit): ValueEventListener {
        val ref = db.getReference("pairs").child(pairId).child("presence")
        val listener = object : ValueEventListener {
            override fun onDataChange(snapshot: DataSnapshot) {
                val onlineFlag = snapshot.child("online").getValue(Boolean::class.java) ?: false
                val ts = snapshot.child("ts").getValue(Long::class.java) ?: 0L
                onChange(onlineFlag && System.currentTimeMillis() - ts < PRESENCE_FRESH_MS)
            }

            override fun onCancelled(error: DatabaseError) {
                onChange(false)
            }
        }
        ref.addValueEventListener(listener)
        return listener
    }

    fun stopObserving(pairId: String, listener: ValueEventListener) {
        db.getReference("pairs").child(pairId).child("presence")
            .removeEventListener(listener)
    }

    companion object {
        private const val PRESENCE_FRESH_MS = 60_000L
    }
}
