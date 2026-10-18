package com.buduping.app.ui

import android.content.Context
import android.os.VibrationEffect
import android.os.Vibrator
import androidx.compose.animation.AnimatedVisibility
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.lazy.grid.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalHapticFeedback
import androidx.compose.ui.hapticfeedback.HapticFeedbackType
import androidx.compose.ui.semantics.*
import androidx.compose.ui.unit.dp
import com.buduping.app.*
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.delay
import kotlinx.coroutines.withContext

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun HomeScreen(prefs: Prefs, repo: FirebaseRepo, isForeground: Boolean, onUnpair: () -> Unit, onChangeCode: () -> Unit) {
    val context = LocalContext.current
    val haptic = LocalHapticFeedback.current
    val pairId = prefs.pairId
    var text by rememberSaveable { mutableStateOf("") }
    var composeOpen by rememberSaveable { mutableStateOf(false) }
    var favorites by remember { mutableStateOf(prefs.favorites) }
    var pendingKey by remember(pairId) { mutableStateOf(prefs.pendingKey) }
    var status by remember(pairId) { mutableStateOf(prefs.deliveryStatus) }
    var desktop by remember(pairId) { mutableStateOf(DesktopState()) }
    var connected by remember { mutableStateOf(false) }
    var offset by remember { mutableLongStateOf(0L) }
    var now by remember { mutableLongStateOf(System.currentTimeMillis()) }
    var settings by remember { mutableStateOf(false) }
    var unpair by remember { mutableStateOf(false) }
    var picker by remember { mutableStateOf<Emotion?>(null) }
    var replyType by remember { mutableStateOf<String?>(null) }
    var funTick by remember { mutableIntStateOf(0) }
    var catalogError by remember { mutableStateOf(false) }
    val catalog by produceState<List<StickerVariant>>(emptyList()) {
        value = withContext(Dispatchers.IO) {
            runCatching { StickerCatalog.load(context) }.getOrElse { catalogError = true; emptyList() }
        }
    }
    LaunchedEffect(isForeground) {
        if (isForeground) while (true) { now = System.currentTimeMillis(); delay(2_000) }
    }
    val age = now + offset - desktop.ts
    val online = connected && desktop.online && age in -5_000L..60_000L

    DisposableEffect(pairId, isForeground) {
        val stop = if (isForeground) repo.observe(pairId,
            onPresence = { desktop = it }, onConnected = { connected = it }, onClockOffset = { offset = it },
            onFeedback = { feedback ->
                if (feedback.eventId == pendingKey && pendingKey.isNotBlank()) {
                    status = feedback.status; prefs.deliveryStatus = status
                }
                if (feedback.replyId.isNotBlank() && feedback.replyId != prefs.lastReplyId) {
                    prefs.lastReplyId = feedback.replyId
                    if (System.currentTimeMillis() + offset - feedback.replyTs in -5_000L..600_000L) {
                        replyType = feedback.replyType
                        (context.getSystemService(Context.VIBRATOR_SERVICE) as? Vibrator)?.vibrate(
                            VibrationEffect.createWaveform(longArrayOf(0, 30, 70, 40), -1))
                    }
                }
            }, onError = { status = "permission_error"; connected = false }) else { {} }
        onDispose { stop() }
    }

    fun send(type: String, variant: String = "") {
        val message = text.trim().take(80)
        if (catalog.isEmpty()) return
        haptic.performHapticFeedback(HapticFeedbackType.LongPress)
        status = "sending"
        val key = repo.sendPing(pairId, type, message, prefs.senderName, variant) { id, error ->
            if (error == null) {
                prefs.bumpFun(type); funTick++
                if (pendingKey == id && status == "sending") { status = "sent"; prefs.deliveryStatus = status }
            } else {
                if (pendingKey == id) { status = "failed"; prefs.deliveryStatus = status }
                if (message.isNotBlank() && text.isBlank()) { text = message; composeOpen = true }
            }
        }
        pendingKey = key; prefs.pendingKey = key; prefs.deliveryStatus = "sending"
        text = ""; composeOpen = false
    }

    val ordered = remember(favorites) { EMOTIONS.sortedBy { e -> favorites.indexOf(e.type).let { if (it < 0) 100 else it } } }
    val summary = remember(funTick) { prefs.funToday().take(3) }
    Scaffold(topBar = {
        TopAppBar(title = { Text("BuduPing", style = MaterialTheme.typography.headlineSmall) }, actions = {
            val connectionLabel = if (online) "PC đang mở" else if (!connected) "Chưa có mạng" else "PC đang nghỉ"
            Row(verticalAlignment = Alignment.CenterVertically,
                modifier = Modifier.semantics(mergeDescendants = true) { contentDescription = connectionLabel }) {
                Box(Modifier.size(8.dp).clip(CircleShape).background(if (online) Color(0xFF3F7F62) else MaterialTheme.colorScheme.outline))
                Spacer(Modifier.width(6.dp))
                Text(connectionLabel, style = MaterialTheme.typography.labelSmall)
            }
            IconButton(onClick = { settings = true }) { Icon(Icons.Default.Settings, "Cài đặt") }
        })
    }) { padding ->
        Column(Modifier.fillMaxSize().padding(padding).padding(horizontal = 16.dp)) {
            Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.SpaceBetween) {
                AssistChip(onClick = { unpair = true }, label = { Text(PairCode.format(pairId)) })
                TextButton(onClick = { composeOpen = !composeOpen }) { Text(if (composeOpen) "Thu gọn" else "Viết lời nhắn") }
            }
            AnimatedVisibility(composeOpen) {
                Column {
                    OutlinedTextField(value = text, onValueChange = { text = it.take(80) },
                        label = { Text("Nhắn gì đó…") }, modifier = Modifier.fillMaxWidth(),
                        singleLine = true, supportingText = { Text("${text.length}/80") })
                    LazyRow(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                        items(PRESET_CHIPS) { chip -> AssistChip(onClick = { text = chip.take(80) }, label = { Text(chip) }) }
                    }
                }
            }
            if (!composeOpen && text.isNotBlank()) {
                Text(text, maxLines = 1, style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.primary, modifier = Modifier.padding(bottom = 8.dp))
            }
            Text("Gửi một chút thương.", style = MaterialTheme.typography.titleLarge, modifier = Modifier.padding(top = 8.dp, bottom = 4.dp))
            Text("Chạm để gửi · giữ để chọn sticker", style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant, modifier = Modifier.padding(bottom = 12.dp))
            if (catalogError) Text("Chưa tải được sticker. Mở lại ứng dụng để thử lại.", color = MaterialTheme.colorScheme.error)
            LazyVerticalGrid(columns = GridCells.Fixed(3), modifier = Modifier.weight(1f),
                horizontalArrangement = Arrangement.spacedBy(10.dp), verticalArrangement = Arrangement.spacedBy(10.dp),
                contentPadding = PaddingValues(bottom = 12.dp)) {
                items(ordered, key = { it.type }) { emotion ->
                    EmotionTile(emotion, catalog.firstOrNull { it.type == emotion.type }, emotion.type in favorites,
                        enabled = catalog.isNotEmpty(), onClick = { send(emotion.type) }, onLongClick = { picker = emotion })
                }
            }
            val statusLabel = when (status) {
                "sending" -> if (connected) "Đang gửi…" else "Đang chờ mạng…"
                "sent" -> if (online) "Đã gửi · chờ PC hiển thị" else "Đã gửi · chờ PC mở"
                "displayed" -> "Đã hiện trên PC"
                "received" -> "PC đã nhận · nội dung đang ẩn"
                "expired" -> "Cảm xúc cũ đã hết hạn"
                "superseded" -> "Đã gộp vào chuỗi cảm xúc"
                "failed" -> "Chưa gửi được · lời nhắn được giữ lại"
                "permission_error" -> "Không đọc được kết nối · kiểm tra Firebase"
                else -> ""
            }
            if (statusLabel.isNotBlank()) Text(statusLabel, modifier = Modifier.fillMaxWidth().padding(vertical = 6.dp)
                .semantics { liveRegion = LiveRegionMode.Polite }, style = MaterialTheme.typography.bodySmall,
                color = if (status in listOf("failed", "permission_error")) MaterialTheme.colorScheme.error else MaterialTheme.colorScheme.primary)
            if (summary.isNotEmpty()) Text("Hôm nay: " + summary.joinToString(" · ") { (type, count) -> "$count ${FUN_LABELS[type] ?: type}" },
                style = MaterialTheme.typography.labelSmall, color = MaterialTheme.colorScheme.onSurfaceVariant,
                modifier = Modifier.padding(bottom = 8.dp))
        }
    }
    picker?.let { emotion ->
        VariantPicker(emotion, catalog.filter { it.type == emotion.type }, emotion.type in favorites,
            onFavorite = {
                favorites = if (emotion.type in favorites) favorites - emotion.type else (favorites + emotion.type).takeLast(4)
                prefs.favorites = favorites
            }, onDismiss = { picker = null }, onSend = { variant -> picker = null; send(emotion.type, variant.id) })
    }
    replyType?.let { type ->
        AlertDialog(onDismissRequest = { replyType = null }, title = { Text("Một cái ôm đáp lại.") },
            text = { StickerArt(catalog.firstOrNull { it.type == type } ?: catalog.firstOrNull { it.type == "hug" },
                Modifier.fillMaxWidth().height(220.dp), "Một cái ôm từ PC", animated = isForeground) },
            confirmButton = { TextButton(onClick = { replyType = null }) { Text("Nhận rồi") } })
    }
    if (settings) SettingsDialog(prefs, onDismiss = { settings = false }, onChangeCode = { settings = false; onChangeCode() })
    if (unpair) AlertDialog(onDismissRequest = { unpair = false }, title = { Text("Huỷ ghép cặp?") },
        text = { Text("Mã ${PairCode.format(pairId)} sẽ bị quên trên điện thoại này.") },
        confirmButton = { TextButton(onClick = onUnpair) { Text("Huỷ cặp") } },
        dismissButton = { TextButton(onClick = { unpair = false }) { Text("Giữ lại") } })
}
