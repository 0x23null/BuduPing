package com.buduping.app.ui

import android.content.Context
import android.os.VibrationEffect
import android.os.Vibrator
import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.tween
import androidx.compose.foundation.shape.RoundedCornerShape
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
    var myCharacter by remember { mutableStateOf(prefs.myCharacter) }
    var characterPicker by remember { mutableStateOf(false) }
    var replyType by remember { mutableStateOf<String?>(null) }
    var funTick by remember { mutableIntStateOf(0) }
    var comboType by remember { mutableStateOf("") }
    var comboCount by remember { mutableIntStateOf(0) }
    var lastTap by remember { mutableLongStateOf(0L) }
    val bursts = remember { mutableStateMapOf<String, Int>() }
    LaunchedEffect(lastTap) { if (lastTap > 0) { delay(2_000); comboCount = 0 } }
    val comboProgress by animateFloatAsState((comboCount / 5f).coerceIn(0f, 1f), tween(250), label = "combo")
    var catalogError by remember { mutableStateOf(false) }
    val catalog by produceState<List<StickerVariant>>(emptyList()) {
        value = withContext(Dispatchers.IO) {
            runCatching { StickerCatalog.load(context) }.getOrElse { catalogError = true; emptyList() }
        }
    }
    LaunchedEffect(myCharacter, catalog) {
        // One-time "who are you" prompt: pops after pairing once the catalog is loaded.
        if (myCharacter == null && catalog.isNotEmpty()) characterPicker = true
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
        val time = System.currentTimeMillis()
        comboCount = if (comboType == type && time - lastTap <= 2_000) comboCount + 1 else 1
        comboType = type; lastTap = time; bursts[type] = (bursts[type] ?: 0) + 1
        status = "sending"
        // Quick tap: pick a variant of MY character (solo first, then duo); the long-press
        // picker sends its own explicit variant. The chosen variant always goes in the payload.
        val chosen = if (variant.isBlank()) StickerCatalog.pickVariant(catalog, type, myCharacter)?.id ?: "" else variant
        val key = repo.sendPing(pairId, type, message, prefs.senderName, chosen) { id, error ->
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
            Box(Modifier.size(32.dp).semantics { contentDescription = connectionLabel }, contentAlignment = Alignment.Center) {
                Box(Modifier.size(10.dp).clip(CircleShape).background(if (online) Color(0xFF3F7F62) else MaterialTheme.colorScheme.outline))
            }
            IconButton(onClick = { settings = true }) { Icon(Icons.Default.Settings, "Cài đặt") }
        })
    }) { padding ->
        Column(Modifier.fillMaxSize().padding(padding).padding(horizontal = 16.dp)) {
            Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.SpaceBetween) {
                AssistChip(onClick = { unpair = true }, label = { Text(PairCode.format(pairId)) })
                Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    if (myCharacter != null) {
                        AssistChip(onClick = { characterPicker = true },
                            leadingIcon = { StickerArt(characterPreview(catalog, myCharacter), Modifier.size(24.dp), null) },
                            label = { Text(characterName(myCharacter)) })
                    }
                    Column(horizontalAlignment = Alignment.End) {
                        Text(prefs.senderName, style = MaterialTheme.typography.labelMedium)
                        Text(if (online) "PC đang mở" else if (!connected) "Chưa có mạng" else "PC đang nghỉ",
                            style = MaterialTheme.typography.labelSmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
                    }
                }
            }
            Surface(shape = RoundedCornerShape(18.dp), color = MaterialTheme.colorScheme.surfaceVariant,
                modifier = Modifier.fillMaxWidth().padding(vertical = 8.dp)) {
                Column(Modifier.padding(14.dp, 12.dp)) {
                    Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                        val label = EMOTIONS.firstOrNull { it.type == comboType }?.label ?: "Cảm xúc"
                        val level = if (comboCount >= 5) 3 else if (comboCount >= 3) 2 else 1
                        val idleText = prefs.senderName + " · " +
                            if (online) "PC đang mở" else if (!connected) "Chưa có mạng" else "PC đang nghỉ"
                        Text(if (comboCount > 0) "$label · Lv$level" else idleText,
                            style = MaterialTheme.typography.labelLarge, color = MaterialTheme.colorScheme.primary)
                        if (comboCount > 0) Text("x$comboCount", style = MaterialTheme.typography.labelSmall)
                    }
                    Spacer(Modifier.height(8.dp))
                    LinearProgressIndicator(progress = { comboProgress }, modifier = Modifier.fillMaxWidth().height(5.dp),
                        trackColor = MaterialTheme.colorScheme.primaryContainer)
                }
            }
            LazyVerticalGrid(columns = GridCells.Fixed(4), modifier = Modifier.weight(1f),
                horizontalArrangement = Arrangement.spacedBy(8.dp), verticalArrangement = Arrangement.spacedBy(8.dp),
                contentPadding = PaddingValues(top = 4.dp, bottom = 12.dp)) {
                items(ordered, key = { it.type }) { emotion ->
                    val preview = if (emotion.type == "love") catalog.firstOrNull { it.id == "love-02" } else null
                    EmotionTile(emotion, preview ?: StickerCatalog.allowedFor(catalog, emotion.type, myCharacter).firstOrNull(),
                        emotion.type in favorites,
                        enabled = catalog.isNotEmpty(), burstToken = bursts[emotion.type] ?: 0,
                        onClick = { send(emotion.type) }, onLongClick = { picker = emotion })
                }
            }
            Surface(shape = RoundedCornerShape(18.dp), color = MaterialTheme.colorScheme.surfaceVariant) {
                Column(Modifier.padding(12.dp)) {
                    OutlinedTextField(value = text, onValueChange = { text = it.take(80) },
                        label = { Text("Lời nhắn", maxLines = 1) }, modifier = Modifier.fillMaxWidth(),
                        singleLine = true, trailingIcon = { Text("${text.length}/80", style = MaterialTheme.typography.labelSmall) })
                    Spacer(Modifier.height(6.dp))
                    LazyRow(horizontalArrangement = Arrangement.spacedBy(6.dp)) {
                        items(PRESET_CHIPS) { chip -> AssistChip(onClick = { text = chip.take(80) }, label = { Text(chip) }) }
                    }
                }
            }
            Text("Giữ một cảm xúc để chọn sticker hoặc ghim.", style = MaterialTheme.typography.labelSmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant, modifier = Modifier.padding(top = 8.dp))
            if (catalogError) Text("Chưa tải được sticker. Mở lại ứng dụng để thử lại.", color = MaterialTheme.colorScheme.error)
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
        VariantPicker(emotion, StickerCatalog.allowedFor(catalog, emotion.type, myCharacter), emotion.type in favorites,
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
    if (characterPicker) CharacterPicker(catalog,
        onPick = { kind -> prefs.myCharacter = kind; myCharacter = kind; characterPicker = false },
        onDismiss = { characterPicker = false })
    if (settings) SettingsDialog(prefs, onDismiss = { settings = false }, onChangeCode = { settings = false; onChangeCode() })
    if (unpair) AlertDialog(onDismissRequest = { unpair = false }, title = { Text("Huỷ ghép cặp?") },
        text = { Text("Mã ${PairCode.format(pairId)} sẽ bị quên trên điện thoại này.") },
        confirmButton = { TextButton(onClick = onUnpair) { Text("Huỷ cặp") } },
        dismissButton = { TextButton(onClick = { unpair = false }) { Text("Giữ lại") } })
}
