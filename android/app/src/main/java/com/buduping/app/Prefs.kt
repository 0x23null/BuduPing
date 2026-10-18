package com.buduping.app

import android.content.Context
import android.content.SharedPreferences
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

/** Tiny local storage via SharedPreferences — no DataStore, no DB. */
class Prefs(context: Context) {

    private val sp: SharedPreferences =
        context.getSharedPreferences("buduping", Context.MODE_PRIVATE)

    var senderName: String
        get() = sp.getString(KEY_NAME, "") ?: ""
        set(value) = sp.edit().putString(KEY_NAME, value).apply()

    var pairId: String
        get() = sp.getString(KEY_PAIR, "") ?: ""
        set(value) = sp.edit().putString(KEY_PAIR, value).apply()

    fun clearPair() {
        sp.edit().remove(KEY_PAIR).apply()
    }

    // --- Fun counters, per day, purely local ---

    private fun today(): String =
        SimpleDateFormat("yyyy-MM-dd", Locale.US).format(Date())

    fun bumpFun(type: String) {
        val key = "fun_${today()}_$type"
        sp.edit().putInt(key, sp.getInt(key, 0) + 1).apply()
    }

    /** (type, count) pairs for today with count > 0. */
    fun funToday(): List<Pair<String, Int>> {
        val prefix = "fun_${today()}_"
        return sp.all
            .filter { (k, v) -> k.startsWith(prefix) && v is Int && v > 0 }
            .map { (k, v) -> k.removePrefix(prefix) to (v as Int) }
            .sortedByDescending { it.second }
    }

    companion object {
        private const val KEY_NAME = "sender_name"
        private const val KEY_PAIR = "pair_id"
    }
}
