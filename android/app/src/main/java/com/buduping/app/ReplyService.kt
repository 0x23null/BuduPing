package com.buduping.app

import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.app.Service
import android.content.Context
import android.content.Intent
import android.content.pm.ServiceInfo
import android.os.Build
import android.os.IBinder
import androidx.core.content.ContextCompat

/**
 * Keeps the pair's feedback listener alive while the app is backgrounded.
 * Before this service existed, the listener lived in the activity: swiping
 * the app away (or the system reaping the process) silently killed it, so
 * hug-backs from the PC produced no notification at all — the owner hit
 * exactly that. A dataSync foreground service is the Android-sanctioned way
 * to hold this tiny connection; it shows one quiet persistent notification
 * (channel importance LOW) that the user can hide in system settings.
 */
class ReplyService : Service() {
    private var stop: (() -> Unit)? = null
    private var observingPair: String = ""
    private var offset: Long = 0

    override fun onBind(intent: Intent?): IBinder? = null

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        val prefs = Prefs(this)
        val pairId = prefs.pairId
        if (pairId.isBlank()) {
            stopSelf()
            return START_NOT_STICKY
        }
        if (Build.VERSION.SDK_INT >= 34) {
            startForeground(PERSISTENT_ID, persistentNotification(),
                ServiceInfo.FOREGROUND_SERVICE_TYPE_DATA_SYNC)
        } else {
            startForeground(PERSISTENT_ID, persistentNotification())
        }
        if (observingPair != pairId) {
            stop?.invoke()
            observingPair = pairId
            stop = FirebaseRepo().observe(pairId,
                onPresence = {},
                onConnected = {},
                onClockOffset = { offset = it },
                onFeedback = { feedback ->
                    if (feedback.replyId.isNotBlank() && feedback.replyId != prefs.lastReplyId &&
                        System.currentTimeMillis() + offset - feedback.replyTs in -5_000L..600_000L &&
                        !AppForeground.isForeground
                    ) {
                        prefs.lastReplyId = feedback.replyId
                        ReplyNotifier.show(this, feedback.replyType)
                    }
                },
                onError = {})
        }
        return START_STICKY
    }

    override fun onDestroy() {
        stop?.invoke()
        stop = null
        super.onDestroy()
    }

    private fun persistentNotification(): Notification {
        val manager = getSystemService(NotificationManager::class.java)
        if (Build.VERSION.SDK_INT >= 26 && manager?.getNotificationChannel(CHANNEL_ID) == null) {
            manager?.createNotificationChannel(
                NotificationChannel(CHANNEL_ID, "Kết nối nền", NotificationManager.IMPORTANCE_LOW).apply {
                    description = "Cho biết BuduPing đang giữ kết nối để nhận ôm đáp"
                })
        }
        val open = Intent(this, MainActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TOP
        }
        val pending = PendingIntent.getActivity(
            this, 0, open, PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE)
        return Notification.Builder(this, CHANNEL_ID)
            .setSmallIcon(R.drawable.ic_heart)
            .setContentTitle("BuduPing đang giữ kết nối")
            .setContentText("Sẵn sàng nhận ôm đáp từ người thương 💌")
            .setContentIntent(pending)
            .setOngoing(true)
            .build()
    }

    companion object {
        private const val CHANNEL_ID = "connection"
        private const val PERSISTENT_ID = 1002

        fun start(context: Context) {
            ContextCompat.startForegroundService(context, Intent(context, ReplyService::class.java))
        }

        fun stop(context: Context) {
            context.stopService(Intent(context, ReplyService::class.java))
        }
    }
}
