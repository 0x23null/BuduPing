package com.buduping.app

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent

/** After a reboot, resume background hug-back listening without the user
 *  having to open the app first (only when a pair exists). */
class BootReceiver : BroadcastReceiver() {
    override fun onReceive(context: Context, intent: Intent) {
        if (intent.action == Intent.ACTION_BOOT_COMPLETED && Prefs(context).pairId.isNotBlank()) {
            ReplyService.start(context)
        }
    }
}
