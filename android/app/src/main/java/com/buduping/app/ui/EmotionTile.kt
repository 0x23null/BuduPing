package com.buduping.app.ui

import androidx.compose.animation.core.*
import androidx.compose.foundation.ExperimentalFoundationApi
import androidx.compose.foundation.Canvas
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.sp
import kotlinx.coroutines.launch
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
fun EmotionTile(emotion: Emotion, variant: StickerVariant?, favorite: Boolean, enabled: Boolean, burstToken: Int,
                onClick: () -> Unit, onLongClick: () -> Unit) {
    val interaction = remember { MutableInteractionSource() }
    val pressed by interaction.collectIsPressedAsState()
    val scale by animateFloatAsState(if (pressed) 0.92f else 1f,
        spring(dampingRatio = Spring.DampingRatioMediumBouncy, stiffness = Spring.StiffnessMedium), label = "touch")
    val burst = remember { Animatable(1f) }
    LaunchedEffect(burstToken) {
        if (burstToken > 0) { burst.snapTo(0f); burst.animateTo(1f, tween(440)) }
    }
    val rose = MaterialTheme.colorScheme.primary
    Box {
    Surface(shape = RoundedCornerShape(16.dp), color = MaterialTheme.colorScheme.surfaceVariant,
        modifier = Modifier.fillMaxWidth().height(if (LocalDensity.current.fontScale > 1.1f) 120.dp else 88.dp).graphicsLayer { scaleX = scale; scaleY = scale }
            .combinedClickable(enabled = enabled, interactionSource = interaction, indication = androidx.compose.foundation.LocalIndication.current,
                role = Role.Button, onClick = onClick, onLongClick = onLongClick,
                onClickLabel = "Gửi ${emotion.label}", onLongClickLabel = "Chọn sticker ${emotion.label}")
            .semantics(mergeDescendants = true) { contentDescription = emotion.label + if (favorite) ", đã ghim" else "" }) {
        Column(Modifier.padding(8.dp), verticalArrangement = Arrangement.Center, horizontalAlignment = Alignment.CenterHorizontally) {
            StickerArt(variant, Modifier.weight(1f).fillMaxWidth())
            Text(emotion.label, style = MaterialTheme.typography.labelMedium, maxLines = 2, textAlign = TextAlign.Center)
            if (favorite) Text("•", style = MaterialTheme.typography.labelSmall, color = MaterialTheme.colorScheme.primary)
        }
    }
    Canvas(Modifier.matchParentSize()) {
        val t = burst.value
        if (t < 1f) {
            val unit = 9.dp.toPx()
            val x = size.width * .5f + size.width * .15f * t
            val y = size.height * .48f - 64.dp.toPx() * t
            val heart = Path().apply {
                moveTo(x, y + unit * .4f)
                cubicTo(x-unit, y-unit*.4f, x-unit*.4f, y-unit*1.1f, x, y-unit*.45f)
                cubicTo(x+unit*.4f, y-unit*1.1f, x+unit, y-unit*.4f, x, y+unit*.4f)
                close()
            }
            drawPath(heart, rose.copy(alpha = 1f - t))
        }
    }
    }

}
