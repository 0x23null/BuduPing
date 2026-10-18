package com.buduping.app.ui

import android.widget.Toast
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.foundation.background
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.interaction.collectIsPressedAsState
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.aspectRatio
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.grid.GridCells
import androidx.compose.foundation.lazy.grid.LazyVerticalGrid
import androidx.compose.foundation.lazy.grid.items
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.AssistChip
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.hapticfeedback.HapticFeedbackType
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalHapticFeedback
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.buduping.app.EMOTIONS
import com.buduping.app.Emotion
import com.buduping.app.FUN_LABELS
import com.buduping.app.FirebaseRepo
import com.buduping.app.PRESET_CHIPS
import com.buduping.app.PairCode
import com.buduping.app.Prefs

/**
 * The one and only main screen — deliberately minimal:
 * compact header (name + presence dot + settings), pair-code pill,
 * optional text + preset chips, the 12-emotion grid, today's fun counter.
 */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun HomeScreen(
    prefs: Prefs,
    repo: FirebaseRepo,
    onUnpair: () -> Unit,
    onChangeCode: () -> Unit,
) {
    val context = LocalContext.current
    val haptic = LocalHapticFeedback.current
    val pairId = prefs.pairId

    var text by remember { mutableStateOf("") }
    var online by remember { mutableStateOf(false) }
    var showUnpairDialog by remember { mutableStateOf(false) }
    var showSettings by remember { mutableStateOf(false) }
    var funTick by remember { mutableIntStateOf(0) }

    DisposableEffect(pairId) {
        val listener = repo.observePresence(pairId) { online = it }
        onDispose { repo.stopObserving(pairId, listener) }
    }

    fun send(type: String) {
        if (!online) {
            Toast.makeText(
                context,
                "Máy tính chưa bật BuduPing — tin sẽ chờ",
                Toast.LENGTH_SHORT,
            ).show()
        }
        val msg = text.trim().take(MAX_TEXT)
        repo.sendPing(pairId, type, msg, prefs.senderName) {
            Toast.makeText(context, "Gửi thất bại, thử lại nha", Toast.LENGTH_SHORT).show()
        }
        prefs.bumpFun(type)
        funTick++
        if (msg.isNotEmpty()) text = ""
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("BuduPing") },
                actions = {
                    // presence dot: green = desktop alive (tray counts), grey = off
                    Box(
                        modifier = Modifier
                            .size(12.dp)
                            .clip(CircleShape)
                            .background(
                                if (online) Color(0xFF22C55E) else Color(0xFF9CA3AF),
                            ),
                    )
                    IconButton(onClick = { showSettings = true }) {
                        Icon(Icons.Default.Settings, contentDescription = "Cài đặt")
                    }
                },
            )
        },
    ) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
                .padding(horizontal = 16.dp),
        ) {
            // Pair-code pill: tapping it asks to unpair (no separate "connected" block).
            AssistChip(
                onClick = { showUnpairDialog = true },
                label = { Text(PairCode.format(pairId)) },
                leadingIcon = { Text("🔗", fontSize = 14.sp) },
            )
            Spacer(Modifier.height(8.dp))

            OutlinedTextField(
                value = text,
                onValueChange = { if (it.length <= MAX_TEXT) text = it },
                modifier = Modifier.fillMaxWidth(),
                placeholder = { Text("Nhắn gì đó… (tuỳ chọn)") },
                singleLine = true,
                supportingText = {
                    Text(
                        "${text.length}/$MAX_TEXT",
                        modifier = Modifier.fillMaxWidth(),
                        style = MaterialTheme.typography.labelSmall,
                    )
                },
            )

            LazyRow(
                horizontalArrangement = Arrangement.spacedBy(8.dp),
                contentPadding = PaddingValues(vertical = 4.dp),
            ) {
                items(PRESET_CHIPS) { chip ->
                    AssistChip(
                        onClick = {
                            text = (if (text.isBlank()) chip else "$text $chip").take(MAX_TEXT)
                        },
                        label = { Text(chip) },
                    )
                }
            }
            Spacer(Modifier.height(8.dp))

            LazyVerticalGrid(
                columns = GridCells.Fixed(3),
                modifier = Modifier.weight(1f),
                horizontalArrangement = Arrangement.spacedBy(10.dp),
                verticalArrangement = Arrangement.spacedBy(10.dp),
            ) {
                items(EMOTIONS, key = { it.type }) { emotion ->
                    EmotionButton(emotion) {
                        // optimistic: bounce + haptic happen instantly, network never blocks the tap
                        haptic.performHapticFeedback(HapticFeedbackType.LongPress)
                        send(emotion.type)
                    }
                }
            }

            // Compact daily fun counter, e.g. "Hôm nay: 5 ôm · 3 yêu"
            val summary = remember(funTick) { prefs.funToday() }
            if (summary.isNotEmpty()) {
                Text(
                    "Hôm nay: " + summary.joinToString(" · ") { (type, count) ->
                        "$count ${FUN_LABELS[type] ?: type}"
                    },
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    modifier = Modifier.padding(vertical = 12.dp),
                )
            } else {
                Spacer(Modifier.height(12.dp))
            }
        }
    }

    if (showUnpairDialog) {
        AlertDialog(
            onDismissRequest = { showUnpairDialog = false },
            title = { Text("Huỷ kết nối?") },
            text = { Text("Mã ${PairCode.format(pairId)} sẽ bị quên trên máy này.") },
            confirmButton = {
                TextButton(onClick = onUnpair) { Text("Huỷ cặp") }
            },
            dismissButton = {
                TextButton(onClick = { showUnpairDialog = false }) { Text("Giữ lại") }
            },
        )
    }

    if (showSettings) {
        SettingsDialog(
            prefs = prefs,
            onDismiss = { showSettings = false },
            onChangeCode = {
                showSettings = false
                onChangeCode()
            },
        )
    }
}

/**
 * One emotion button. Press feedback is a transform-only scale animation
 * (60fps, GPU-cheap) — the tap feels instant even before the network answers.
 */
@Composable
private fun EmotionButton(emotion: Emotion, onClick: () -> Unit) {
    val interaction = remember { MutableInteractionSource() }
    val pressed by interaction.collectIsPressedAsState()
    val scale by animateFloatAsState(
        targetValue = if (pressed) 0.85f else 1f,
        label = "press",
    )
    Surface(
        onClick = onClick,
        modifier = Modifier
            .aspectRatio(1f)
            .graphicsLayer {
                scaleX = scale
                scaleY = scale
            },
        shape = RoundedCornerShape(20.dp),
        color = MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.6f),
        interactionSource = interaction,
    ) {
        Column(
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.Center,
            modifier = Modifier.fillMaxSize(),
        ) {
            Text(emotion.emoji, fontSize = 38.sp)
            Spacer(Modifier.height(2.dp))
            Text(emotion.label, style = MaterialTheme.typography.labelMedium)
        }
    }
}

private const val MAX_TEXT = 80
