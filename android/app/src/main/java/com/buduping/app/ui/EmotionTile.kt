package com.buduping.app.ui

import androidx.compose.animation.core.*
import androidx.compose.foundation.ExperimentalFoundationApi
import androidx.compose.foundation.combinedClickable
import androidx.compose.foundation.interaction.*
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.semantics.*
import androidx.compose.ui.unit.dp
import com.buduping.app.Emotion
import com.buduping.app.StickerVariant

@OptIn(ExperimentalFoundationApi::class)
@Composable
fun EmotionTile(emotion: Emotion, variant: StickerVariant?, favorite: Boolean, enabled: Boolean,
                onClick: () -> Unit, onLongClick: () -> Unit) {
    val interaction = remember { MutableInteractionSource() }
    val pressed by interaction.collectIsPressedAsState()
    val scale by animateFloatAsState(if (pressed) 0.92f else 1f,
        spring(dampingRatio = Spring.DampingRatioMediumBouncy, stiffness = Spring.StiffnessMedium), label = "touch")
    Surface(shape = RoundedCornerShape(20.dp), color = MaterialTheme.colorScheme.surfaceVariant,
        modifier = Modifier.fillMaxWidth().aspectRatio(0.92f).graphicsLayer { scaleX = scale; scaleY = scale }
            .combinedClickable(enabled = enabled, interactionSource = interaction, indication = androidx.compose.foundation.LocalIndication.current,
                role = Role.Button, onClick = onClick, onLongClick = onLongClick,
                onClickLabel = "Gửi ${emotion.label}", onLongClickLabel = "Chọn sticker ${emotion.label}")
            .semantics(mergeDescendants = true) { contentDescription = emotion.label + if (favorite) ", đã ghim" else "" }) {
        Column(Modifier.padding(8.dp), verticalArrangement = Arrangement.Center, horizontalAlignment = Alignment.CenterHorizontally) {
            StickerArt(variant, Modifier.weight(1f).fillMaxWidth())
            Text(emotion.label, style = MaterialTheme.typography.labelLarge, maxLines = 1)
            if (favorite) Text("•", style = MaterialTheme.typography.labelSmall, color = MaterialTheme.colorScheme.primary)
        }
    }
}
