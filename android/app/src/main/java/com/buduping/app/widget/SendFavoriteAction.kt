package com.buduping.app.widget

import android.content.Context
import androidx.datastore.preferences.core.MutablePreferences
import androidx.glance.GlanceId
import androidx.glance.action.ActionParameters
import androidx.glance.appwidget.action.ActionCallback
import androidx.glance.appwidget.state.updateAppWidgetState
import com.buduping.app.FirebaseRepo
import com.buduping.app.Prefs
import com.buduping.app.ReplyHaptics
import com.buduping.app.StickerCatalog
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

/** Quick tap from the widget: same payload as a quick tap in the app (auto variant of my character, no text). */
class SendFavoriteAction : ActionCallback {
    override suspend fun onAction(context: Context, glanceId: GlanceId, parameters: ActionParameters) {
        val type = parameters[TypeKey] ?: return
        val prefs = Prefs(context)
        val pairId = prefs.pairId
        if (pairId.isBlank()) { FavoritesWidget().update(context, glanceId); return }
        ReplyHaptics.tick(context)
        val catalog = withContext(Dispatchers.IO) { runCatching { StickerCatalog.load(context) }.getOrDefault(emptyList()) }
        val variant = StickerCatalog.pickVariant(catalog, type, prefs.myCharacter)?.id
        if (variant == null) { setStatus(context, glanceId, "unavailable:$type"); return }
        setStatus(context, glanceId, "sending:$type")
        val outcome = FirebaseRepo().sendPingAndWait(pairId, type, prefs.senderName, variant, prefs.myCharacter ?: "")
        if (outcome != FirebaseRepo.SendOutcome.FAILED) prefs.bumpFun(type)
        setStatus(context, glanceId, when (outcome) {
            FirebaseRepo.SendOutcome.SENT -> "sent:$type:${FavoritesWidget.stamp()}"
            FirebaseRepo.SendOutcome.QUEUED -> "queued:$type"
            FirebaseRepo.SendOutcome.FAILED -> "failed:$type"
        })
    }

    private suspend fun setStatus(context: Context, glanceId: GlanceId, value: String) {
        updateAppWidgetState(context, glanceId) { state: MutablePreferences -> state[FavoritesWidget.StatusKey] = value }
        FavoritesWidget().update(context, glanceId)
    }

    companion object {
        val TypeKey = ActionParameters.Key<String>("type")
    }
}
