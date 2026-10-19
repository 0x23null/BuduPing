package com.buduping.app

import android.Manifest
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import android.os.Build

/**
 * Hug-back notifications. Before this, the app never asked for
 * POST_NOTIFICATIONS and never posted anything — a reply from the PC only
 * appeared if the phone app happened to be open. Now: the permission is
 * requested properly (once, from HomeScreen; re-enable lives in Settings)
 * and a fresh reply posts a system notification whenever the app is not in
 * the foreground.
 */
object ReplyNotifier {
    private const val CHANNEL_ID = "hug_back"
    private const val NOTIF_ID = 1001

    fun isGranted(context: Context): Boolean =
        Build.VERSION.SDK_INT < 33 ||
            context.checkSelfPermission(Manifest.permission.POST_NOTIFICATIONS) == PackageManager.PERMISSION_GRANTED

    fun show(context: Context, replyType: String) {
        if (!isGranted(context)) return
        val manager = context.getSystemService(NotificationManager::class.java) ?: return
        if (Build.VERSION.SDK_INT >= 26 && manager.getNotificationChannel(CHANNEL_ID) == null) {
            manager.createNotificationChannel(
                NotificationChannel(CHANNEL_ID, "Ôm đáp lại", NotificationManager.IMPORTANCE_HIGH).apply {
                    description = "Khi người thương ôm hoặc đáp lại từ PC"
                })
        }
        val label = EMOTIONS.firstOrNull { it.type == replyType }?.label ?: "Ôm"
        val open = Intent(context, MainActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TOP
        }
        val pending = PendingIntent.getActivity(
            context, 0, open,
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE)
        val notification = android.app.Notification.Builder(context, CHANNEL_ID)
            .setSmallIcon(R.drawable.ic_heart)
            .setContentTitle("Người thương vừa đáp lại nè 💌")
            .setContentText("$label từ PC — mở BuduPing xem nhé")
            .setAutoCancel(true)
            .setContentIntent(pending)
            .build()
        manager.notify(NOTIF_ID, notification)
    }
}
