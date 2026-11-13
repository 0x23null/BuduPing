package com.buduping.app.widget

import android.content.Context
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.glance.GlanceId
import androidx.glance.GlanceModifier
import androidx.glance.action.actionParametersOf
import androidx.glance.action.actionStartActivity
import androidx.glance.action.clickable
import androidx.glance.appwidget.GlanceAppWidget
import androidx.glance.appwidget.GlanceAppWidgetReceiver
import androidx.glance.appwidget.SizeMode
import androidx.glance.appwidget.action.actionRunCallback
import androidx.glance.appwidget.cornerRadius
import androidx.glance.appwidget.provideContent
import androidx.glance.appwidget.updateAll
import androidx.glance.background
import androidx.glance.color.ColorProvider
import androidx.glance.currentState
import androidx.glance.layout.Alignment
import androidx.glance.layout.Box
import androidx.glance.layout.Column
import androidx.glance.layout.Row
import androidx.glance.layout.Spacer
import androidx.glance.layout.fillMaxSize
import androidx.glance.layout.fillMaxWidth
import androidx.glance.layout.height
import androidx.glance.layout.padding
import androidx.glance.layout.width
import androidx.glance.text.FontWeight
import androidx.glance.text.Text
import androidx.glance.text.TextAlign
import androidx.glance.text.TextStyle
import com.buduping.app.EMOTIONS
import com.buduping.app.Emotion
import com.buduping.app.MainActivity
import com.buduping.app.Prefs
import com.buduping.app.StickerCatalog
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

/**
 * Home-screen strip of the pinned moods: one tap sends without opening the app.
 * Reads the same Prefs as the app; the last outcome is kept in widget state so it survives relaunches.
 */
class FavoritesWidget : GlanceAppWidget() {
    override val sizeMode: SizeMode = SizeMode.Exact

    data class Snapshot(val paired: Boolean, val name: String, val moods: List<Emotion>)

    override suspend fun provideGlance(context: Context, id: GlanceId) {
        val snapshot = withContext(Dispatchers.IO) { snapshot(context) }
        provideContent { Content(snapshot, currentState()) }
    }

    @Composable
    private fun Content(snapshot: Snapshot, state: Preferences) {
        val bg = ColorProvider(Color(0xFFFFF7FA), Color(0xFF211B18))
        val tile = ColorProvider(Color(0xFFF5E5DB), Color(0xFF342923))
        val ink = ColorProvider(Color(0xFF382923), Color(0xFFF7E8DF))
        val muted = ColorProvider(Color(0xFF746057), Color(0xFFD4BEB1))
        val rose = ColorProvider(Color(0xFFA93F65), Color(0xFFFFAEC9))
        Box(GlanceModifier.fillMaxSize().background(bg).cornerRadius(20.dp).padding(10.dp)) {
            if (!snapshot.paired) {
                Box(GlanceModifier.fillMaxSize().clickable(actionStartActivity<MainActivity>()), contentAlignment = Alignment.Center) {
                    Text("Mở BuduPing để ghép cặp", style = TextStyle(color = rose, fontSize = 14.sp, fontWeight = FontWeight.Medium))
                }
                return@Box
            }
            Column(GlanceModifier.fillMaxSize()) {
                Row(GlanceModifier.fillMaxWidth().clickable(actionStartActivity<MainActivity>()), verticalAlignment = Alignment.CenterVertically) {
                    Text("BuduPing", style = TextStyle(color = rose, fontSize = 11.sp, fontWeight = FontWeight.Bold))
                    Spacer(GlanceModifier.width(8.dp))
                    Text(statusLine(state), style = TextStyle(color = muted, fontSize = 11.sp), maxLines = 1)
                }
                Spacer(GlanceModifier.height(6.dp))
                Row(GlanceModifier.fillMaxWidth().defaultWeight()) {
                    snapshot.moods.forEachIndexed { index, mood ->
                        if (index > 0) Spacer(GlanceModifier.width(6.dp))
                        Column(
                            modifier = GlanceModifier.defaultWeight().fillMaxSize().background(tile).cornerRadius(14.dp)
                                .clickable(actionRunCallback<SendFavoriteAction>(actionParametersOf(SendFavoriteAction.TypeKey to mood.type))),
                            verticalAlignment = Alignment.CenterVertically, horizontalAlignment = Alignment.CenterHorizontally,
                        ) {
                            Text(mood.emoji, style = TextStyle(fontSize = 22.sp, textAlign = TextAlign.Center))
                            Text(mood.label, style = TextStyle(color = ink, fontSize = 11.sp, textAlign = TextAlign.Center), maxLines = 1)
                        }
                    }
                }
            }
        }
    }

    private fun statusLine(state: Preferences): String {
        val raw = state[StatusKey] ?: return "Chạm để gửi ngay"
        val parts = raw.split(':')
        val label = EMOTIONS.firstOrNull { it.type == parts.getOrNull(1) }?.label ?: ""
        return when (parts[0]) {
            "sending" -> "Đang gửi $label…"
            "sent" -> "Đã gửi $label · ${parts.getOrNull(2) ?: ""}".trim()
            "queued" -> "Đã xếp hàng $label · chờ mạng"
            "unavailable" -> "Chưa có sticker $label"
            "failed" -> "Chưa gửi được $label"
            else -> "Chạm để gửi ngay"
        }
    }

    companion object {
        val StatusKey = stringPreferencesKey("status")
        private val clock = SimpleDateFormat("HH:mm", Locale.US)
        fun stamp(): String = clock.format(Date())

        /** Pinned moods first; falls back to the grid order so the strip is never empty. Only moods the catalog can draw. */
        fun snapshot(context: Context): Snapshot {
            val prefs = Prefs(context)
            if (prefs.pairId.isBlank()) return Snapshot(false, prefs.senderName, emptyList())
            val catalog = runCatching { StickerCatalog.load(context) }.getOrDefault(emptyList())
            val me = prefs.myCharacter
            val eligible = EMOTIONS.filter { catalog.isEmpty() || StickerCatalog.allowedFor(catalog, it.type, me).isNotEmpty() }
            val pinned = prefs.favorites.mapNotNull { type -> eligible.firstOrNull { it.type == type } }
            val moods = (pinned + eligible.filter { it !in pinned }).take(4)
            return Snapshot(true, prefs.senderName, moods)
        }

        /** Fire-and-forget re-render after favorites, character or pairing change in the app. */
        fun refresh(context: Context) {
            val app = context.applicationContext
            CoroutineScope(Dispatchers.Default).launch { runCatching { FavoritesWidget().updateAll(app) } }
        }
    }
}

class FavoritesWidgetReceiver : GlanceAppWidgetReceiver() {
    override val glanceAppWidget: GlanceAppWidget = FavoritesWidget()
}
