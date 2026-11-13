package com.buduping.app

import android.content.Context
import android.os.Build
import android.os.VibrationEffect
import android.os.Vibrator
import android.os.VibratorManager

/** One vibration "signature" per mood so a reply from the PC can be felt before it is seen. */
object ReplyHaptics {
    fun pattern(type: String): LongArray = when (type) {
        "love", "kiss" -> longArrayOf(0, 45, 90, 45, 280, 45, 90, 45)   // double heartbeat
        "hug" -> longArrayOf(0, 110, 60, 220)                            // squeeze
        "miss" -> longArrayOf(0, 60, 220, 60)                            // two slow sighs
        "tease" -> longArrayOf(0, 22, 50, 22, 50, 22)                    // poke poke poke
        "angry" -> longArrayOf(0, 140, 70, 140)                          // stomp stomp
        "sad" -> longArrayOf(0, 90, 300, 90)
        "sorry" -> longArrayOf(0, 30, 90, 30, 90, 30)
        "funny" -> longArrayOf(0, 20, 40, 20, 40, 20, 40, 70)            // giggle
        "cheer" -> longArrayOf(0, 35, 45, 35, 45, 110)                   // rising
        "hungry" -> longArrayOf(0, 70, 110, 70)                          // tummy rumble
        "goodnight" -> longArrayOf(0, 220)                               // one soft long
        else -> longArrayOf(0, 30, 70, 40)
    }

    fun play(context: Context, type: String) {
        val vibrator = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S)
            (context.getSystemService(Context.VIBRATOR_MANAGER_SERVICE) as? VibratorManager)?.defaultVibrator
        else @Suppress("DEPRECATION") context.getSystemService(Context.VIBRATOR_SERVICE) as? Vibrator
        if (vibrator?.hasVibrator() != true) return
        runCatching { vibrator.vibrate(VibrationEffect.createWaveform(pattern(type), -1)) }
    }

    fun tick(context: Context) {
        val vibrator = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S)
            (context.getSystemService(Context.VIBRATOR_MANAGER_SERVICE) as? VibratorManager)?.defaultVibrator
        else @Suppress("DEPRECATION") context.getSystemService(Context.VIBRATOR_SERVICE) as? Vibrator
        if (vibrator?.hasVibrator() != true) return
        runCatching { vibrator.vibrate(VibrationEffect.createOneShot(18, VibrationEffect.DEFAULT_AMPLITUDE)) }
    }
}
