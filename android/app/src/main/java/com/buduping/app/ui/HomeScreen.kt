package com.buduping.app.ui

import android.Manifest
import android.content.Context
import android.os.Build
import android.os.VibrationEffect
import android.os.Vibrator
import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.spring
import androidx.compose.animation.core.tween
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.interaction.collectIsPressedAsState
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
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.scale
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalHapticFeedback
import androidx.compose.ui.hapticfeedback.HapticFeedbackType
import androidx.compose.ui.semantics.*
import androidx.compose.ui.unit.dp
import com.buduping.app.*
import com.buduping.app.R
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
    var replyVariant by remember { mutableStateOf("") }
    var funTick by remember { mutableIntStateOf(0) }
    var comboType by remember { mutableStateOf("") }
    var comboCount by remember { mutableIntStateOf(0) }
    var lastTap by remember { mutableLongStateOf(0L) }
    val bursts = remember { mutableStateMapOf<String, Int>() }
    // Notification permission, asked properly once (Android 13+). Denied?
    // Settings has a re-enable row. Without it a hug-back only shows in-app.
    var notifGranted by remember { mutableStateOf(ReplyNotifier.isGranted(context)) }
    val notifLauncher = rememberLauncherForActivityResult(ActivityResultContracts.RequestPermission()) { granted ->
        notifGranted = granted
    }
    LaunchedEffect(Unit) {
        if (Build.VERSION.SDK_INT >= 33 && !notifGranted && !prefs.notifAsked) {
            prefs.notifAsked = true
            notifLauncher.launch(Manifest.permission.POST_NOTIFICATIONS)
        }
    }
    val foregroundNow by rememberUpdatedState(isForeground)
    // Keep the background reply listener (foreground service) running while paired.
    LaunchedEffect(pairId) { if (pairId.isNotBlank()) ReplyService.start(context) }
    LaunchedEffect(lastTap) { if (lastTap > 0) { delay(2_000); comboCount = 0 } }
    val comboProgress by animateFloatAsState((comboCount / 8f).coerceIn(0f, 1f),
        spring(dampingRatio = 0.5f, stiffness = 320f), label = "combo")
    // The bar renders this snapshot so its exit animation does not flash "x0".
    var shownComboType by remember { mutableStateOf("") }
    var shownComboCount by remember { mutableIntStateOf(0) }
    LaunchedEffect(comboCount) {
        if (comboCount > 0) { shownComboType = comboType; shownComboCount = comboCount }
    }
    val barPulse = remember { androidx.compose.animation.core.Animatable(1f) }
    LaunchedEffect(comboCount) {
        if (comboCount >= 2) {
            barPulse.snapTo(1.05f)
            barPulse.animateTo(1f, spring(dampingRatio = 0.35f, stiffness = 260f))
        }
    }
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

    DisposableEffect(pairId) {
        val stop = repo.observe(pairId,
            onPresence = { desktop = it }, onConnected = { connected = it }, onClockOffset = { offset = it },
            onFeedback = { feedback ->
                if (feedback.eventId == pendingKey && pendingKey.isNotBlank()) {
                    status = feedback.status; prefs.deliveryStatus = status
                }
                if (feedback.replyId.isNotBlank() && feedback.replyId != prefs.lastReplyId) {
                    prefs.lastReplyId = feedback.replyId
                    if (System.currentTimeMillis() + offset - feedback.replyTs in -5_000L..600_000L) {
                        if (foregroundNow) {
                            replyType = feedback.replyType; replyVariant = feedback.replyVariant
                            (context.getSystemService(Context.VIBRATOR_SERVICE) as? Vibrator)?.vibrate(
                                VibrationEffect.createWaveform(longArrayOf(0, 30, 70, 40), -1))
                        } else {
                            ReplyNotifier.show(context, feedback.replyType)
                        }
                    }
                }
            }, onError = { status = "permission_error"; connected = false })
        onDispose { stop() }
    }

    fun send(type: String, variant: String = "") {
        val message = text.trim().take(80)
        if (catalog.isEmpty()) return
        val chosen = if (variant.isBlank()) StickerCatalog.pickVariant(catalog, type, myCharacter)?.id
            else StickerCatalog.allowedFor(catalog, type, myCharacter).firstOrNull { it.id == variant }?.id
        if (chosen == null) { status = "unavailable"; return }
        haptic.performHapticFeedback(HapticFeedbackType.LongPress)
        val time = System.currentTimeMillis()
        comboCount = if (comboType == type && time - lastTap <= 2_000) comboCount + 1 else 1
        comboType = type; lastTap = time; bursts[type] = (bursts[type] ?: 0) + 1
        status = "sending"
        // Quick tap: pick a variant of MY character (solo first, then duo); the long-press
        // picker sends its own explicit variant. The chosen variant always goes in the payload.
        val key = repo.sendPing(pairId, type, message, prefs.senderName, chosen, myCharacter ?: "", variant.isNotBlank()) { id, error ->
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
    Scaffold(modifier = Modifier.testTag("homeScreen"), containerColor = Color(0xFF251812), topBar = {
        TopAppBar(title = { Text("BuduPing", style = MaterialTheme.typography.headlineSmall) }, actions = {
            val connectionLabel = if (online) "PC đang mở" else if (!connected) "Chưa có mạng" else "PC đang nghỉ"
            Box(Modifier.size(32.dp).semantics { contentDescription = connectionLabel }, contentAlignment = Alignment.Center) {
                Box(Modifier.size(10.dp).clip(CircleShape).background(if (online) Color(0xFF3F7F62) else MaterialTheme.colorScheme.outline))
            }
            IconButton(onClick = { settings = true }) { Icon(Icons.Default.Settings, "Cài đặt") }
        })
    }) { padding ->
        Column(Modifier.fillMaxSize().padding(padding)
            .background(Brush.verticalGradient(listOf(Color(0xFF251812), Color(0xFF180F0A))))
            .padding(horizontal = 16.dp)) {
            Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.SpaceBetween) {
                AssistChip(onClick = { unpair = true }, label = { Text(PairCode.format(pairId)) })
                Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    if (myCharacter != null) {
                        AssistChip(onClick = { characterPicker = true },
                            leadingIcon = { StickerArt(characterPreview(catalog, myCharacter), Modifier.size(24.dp), null) },
                            label = { Text(characterName(myCharacter)) })
                    }
                }
            }
            AnimatedVisibility(visible = comboCount > 0,
                enter = androidx.compose.animation.fadeIn(tween(160)) +
                    androidx.compose.animation.expandVertically(tween(240), expandFrom = Alignment.Top),
                exit = androidx.compose.animation.fadeOut(tween(220)) +
                    androidx.compose.animation.shrinkVertically(tween(280), shrinkTowards = Alignment.Top)) {
            val level = if (shownComboCount >= 8) 4 else if (shownComboCount >= 5) 3 else if (shownComboCount >= 3) 2 else 1
            val levelName = listOf("Nhẹ nhàng", "Ấm áp", "Nồng nhiệt", "Bùng cháy")[level - 1]
            val moodLabel = EMOTIONS.firstOrNull { it.type == shownComboType }?.label ?: "Cảm xúc"
            val gradStart by androidx.compose.animation.animateColorAsState(
                when (level) { 4 -> Color(0xFFFF6A3D); 3 -> Color(0xFFFF7A59); else -> Color(0xFFF77CA6) },
                tween(350), label = "gradStart")
            val gradEnd by androidx.compose.animation.animateColorAsState(
                if (level >= 3) Color(0xFFFFD23F) else Color(0xFFFFB35C), tween(350), label = "gradEnd")
            Surface(shape = RoundedCornerShape(20.dp), color = MaterialTheme.colorScheme.surfaceVariant,
                modifier = Modifier.fillMaxWidth().padding(vertical = 8.dp)
                    .graphicsLayer { scaleX = barPulse.value; scaleY = barPulse.value }) {
                Column(Modifier.padding(14.dp, 12.dp)) {
                    Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
                        Text("$moodLabel · $levelName",
                            style = MaterialTheme.typography.labelLarge, fontFamily = BalooFamily,
                            color = MaterialTheme.colorScheme.primary,
                            modifier = Modifier.weight(1f), maxLines = 1, overflow = androidx.compose.ui.text.style.TextOverflow.Ellipsis)
                        Text("Lv$level", style = MaterialTheme.typography.labelSmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant)
                        Spacer(Modifier.width(8.dp))
                        Box(Modifier.clip(RoundedCornerShape(50))
                            .background(Brush.horizontalGradient(listOf(gradStart, gradEnd)))
                            .padding(horizontal = 10.dp, vertical = 3.dp)) {
                            Text("x$shownComboCount", style = MaterialTheme.typography.labelLarge,
                                fontFamily = BalooFamily, color = Color.White)
                        }
                    }
                    Spacer(Modifier.height(10.dp))
                    Box(Modifier.fillMaxWidth().height(10.dp).clip(RoundedCornerShape(50))
                        .background(Color.White.copy(alpha = 0.08f))) {
                        Box(Modifier.fillMaxWidth(comboProgress).fillMaxHeight().clip(RoundedCornerShape(50))
                            .background(Brush.horizontalGradient(listOf(gradStart, gradEnd))))
                    }
                }
            }
            }
            LazyVerticalGrid(columns = GridCells.Fixed(4), modifier = Modifier.weight(1f),
                horizontalArrangement = Arrangement.spacedBy(8.dp), verticalArrangement = Arrangement.spacedBy(8.dp),
                contentPadding = PaddingValues(top = 4.dp, bottom = 12.dp)) {
                items(ordered, key = { it.type }) { emotion ->
                    val allowed = StickerCatalog.allowedFor(catalog, emotion.type, myCharacter)
                    EmotionTile(emotion, StickerCatalog.previewFor(catalog, emotion.type, myCharacter),
                        emotion.type in favorites,
                        enabled = allowed.isNotEmpty(), burstToken = bursts[emotion.type] ?: 0,
                        onClick = { send(emotion.type) }, onLongClick = { picker = emotion })
                }
            }
            Surface(shape = RoundedCornerShape(18.dp), color = MaterialTheme.colorScheme.surfaceVariant) {
                Column(Modifier.padding(12.dp)) {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        OutlinedTextField(value = text, onValueChange = { text = it.take(80) },
                            label = { Text("Lời nhắn", maxLines = 1) }, modifier = Modifier.weight(1f),
                            singleLine = true, trailingIcon = { Text("${text.length}/80", style = MaterialTheme.typography.labelSmall) })
                        Spacer(Modifier.width(10.dp))
                        SendButton { send("love") }
                    }
                    Spacer(Modifier.height(6.dp))
                    LazyRow(horizontalArrangement = Arrangement.spacedBy(6.dp)) {
                        items(PRESET_CHIPS) { chip -> AssistChip(onClick = { text = chip.take(80) }, label = { Text(chip) }) }
                    }
                }
            }
            Text("Giữ một cảm xúc để chọn sticker hoặc ghim.", style = MaterialTheme.typography.labelSmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant, modifier = Modifier.padding(top = 8.dp))
            if (catalogError) Text("Chưa tải được sticker. Mở lại ứng dụng để thử lại.", color = MaterialTheme.colorScheme.error)
            // Only problems earn a line here — the resting "Đã hiện trên PC"
            // status was noise the owner asked to remove.
            val statusLabel = when (status) {
                "unavailable" -> "Chưa có sticker cho nhân vật này. Chọn cảm xúc khác nhé."
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
            text = { StickerArt(catalog.firstOrNull { it.id == replyVariant } ?: catalog.firstOrNull { it.type == type },
                Modifier.fillMaxWidth().height(220.dp), "Một cái ôm từ PC", animated = isForeground) },
            confirmButton = { TextButton(onClick = { replyType = null }) { Text("Nhận rồi") } })
    }
    if (characterPicker) CharacterPicker(catalog,
        onPick = { kind -> prefs.myCharacter = kind; myCharacter = kind; comboCount = 0; comboType = ""; lastTap = 0; characterPicker = false },
        onDismiss = { characterPicker = false })
    if (settings) SettingsDialog(prefs, onDismiss = { settings = false }, onChangeCode = { settings = false; onChangeCode() })
    if (unpair) AlertDialog(onDismissRequest = { unpair = false }, title = { Text("Huỷ ghép cặp?") },
        text = { Text("Mã ${PairCode.format(pairId)} sẽ bị quên trên điện thoại này.") },
        confirmButton = { TextButton(onClick = onUnpair) { Text("Huỷ cặp") } },
        dismissButton = { TextButton(onClick = { unpair = false }) { Text("Giữ lại") } })
}

/**
 * The designed send button: a rose-gradient circle with a white paper plane —
 * replaces the plain default look the owner called out. Sends the composed
 * message riding a "love" ping (a bare tap with no text is just a love tap).
 */
@Composable
private fun SendButton(onClick: () -> Unit) {
    val interaction = remember { MutableInteractionSource() }
    val pressed by interaction.collectIsPressedAsState()
    val scale by animateFloatAsState(if (pressed) 0.84f else 1f, label = "sendScale")
    Box(
        Modifier.size(48.dp).scale(scale).clip(CircleShape)
            .background(Brush.linearGradient(listOf(Color(0xFFF77CA6), Color(0xFFCE4A7C))))
            .clickable(interactionSource = interaction, indication = null) { onClick() },
        contentAlignment = Alignment.Center
    ) {
        Icon(painterResource(R.drawable.ic_send), contentDescription = "Gửi lời nhắn",
            tint = Color.White, modifier = Modifier.size(22.dp))
    }
}
