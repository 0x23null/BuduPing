package com.buduping.app.ui

import androidx.compose.animation.core.*
import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.ExperimentalFoundationApi
import androidx.compose.foundation.Canvas
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.text.style.TextAlign
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

/** Soft pastel identity per mood — tiles stop being one flat dark slab. */
private val PASTELS = mapOf(
    "hello" to Color(0xFFFFD98A), "miss" to Color(0xFFF4A9C4), "love" to Color(0xFFFF9EBB),
    "hug" to Color(0xFFFFBE96), "kiss" to Color(0xFFF78FB8), "tease" to Color(0xFFC4A8F0),
    "angry" to Color(0xFFF08D7E), "sad" to Color(0xFF92B8F5), "sorry" to Color(0xFFF5D491),
    "funny" to Color(0xFFF7E07E), "cheer" to Color(0xFF96DCA8), "hungry" to Color(0xFFFFC478),
    "goodnight" to Color(0xFF9AA1E8), "sleepy" to Color(0xFFB3A8EE), "wow" to Color(0xFF82DCD2),
    "shy" to Color(0xFFFFABC7),
)

private fun mix(a: Color, b: Color, f: Float): Color = Color(
    red = a.red + (b.red - a.red) * f,
    green = a.green + (b.green - a.green) * f,
    blue = a.blue + (b.blue - a.blue) * f,
    alpha = 1f,
)

@OptIn(ExperimentalFoundationApi::class)
@Composable
fun EmotionTile(emotion: Emotion, variant: StickerVariant?, favorite: Boolean, enabled: Boolean, burstToken: Int,
                onClick: () -> Unit, onLongClick: () -> Unit) {
    val interaction = remember { MutableInteractionSource() }
    val pressed by interaction.collectIsPressedAsState()
    val scale by animateFloatAsState(if (pressed) 0.88f else 1f,
        spring(dampingRatio = 0.42f, stiffness = Spring.StiffnessMedium), label = "touch")
    val burst = remember { Animatable(1f) }
    val pop = remember { Animatable(1f) }
    val scope = rememberCoroutineScope()
    LaunchedEffect(burstToken) {
        if (burstToken > 0) {
            burst.snapTo(0f); burst.animateTo(1f, tween(520))
            scope.launch { pop.snapTo(1.22f); pop.animateTo(1f, spring(dampingRatio = 0.38f, stiffness = 320f)) }
        }
    }
    val rose = MaterialTheme.colorScheme.primary
    val pastel = PASTELS[emotion.type] ?: rose
    val tileColor = mix(MaterialTheme.colorScheme.surfaceVariant, pastel, if (enabled) 0.30f else 0.10f)
    Box {
    Surface(shape = RoundedCornerShape(22.dp), color = tileColor,
        border = if (favorite) BorderStroke(1.5.dp, Brush.linearGradient(listOf(Color(0xFFF77CA6), Color(0xFFFFB35C)))) else null,
        modifier = Modifier.fillMaxWidth().height(if (LocalDensity.current.fontScale > 1.1f) 122.dp else 92.dp).graphicsLayer { scaleX = scale; scaleY = scale }
            .combinedClickable(enabled = enabled, interactionSource = interaction, indication = androidx.compose.foundation.LocalIndication.current,
                role = Role.Button, onClick = onClick, onLongClick = onLongClick,
                onClickLabel = "Gửi ${emotion.label}", onLongClickLabel = "Chọn sticker ${emotion.label}")
            .semantics(mergeDescendants = true) { contentDescription = emotion.label + if (favorite) ", đã ghim" else "" }) {
        Column(Modifier.padding(8.dp), verticalArrangement = Arrangement.Center, horizontalAlignment = Alignment.CenterHorizontally) {
            Box(Modifier.weight(1f).fillMaxWidth().graphicsLayer { scaleX = pop.value; scaleY = pop.value },
                contentAlignment = Alignment.Center) {
                StickerArt(variant, Modifier.fillMaxSize())
            }
            Text(emotion.label, style = MaterialTheme.typography.labelMedium, fontFamily = BalooFamily, maxLines = 2, textAlign = TextAlign.Center)
            if (!enabled) Text("Chưa có", style = MaterialTheme.typography.labelSmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
        }
    }
    Canvas(Modifier.matchParentSize()) {
        val t = burst.value
        if (t < 1f) {
            // A little trio of hearts floats up on every tap — spam feels alive.
            val hearts = listOf(
                Triple(-0.24f, 7.dp.toPx(), 54.dp.toPx()),
                Triple(0.02f, 10.dp.toPx(), 76.dp.toPx()),
                Triple(0.27f, 8.dp.toPx(), 62.dp.toPx()),
            )
            for ((dx, unit, rise) in hearts) {
                val sway = (kotlin.math.sin(t * 5.0 + dx * 20.0) * 3.0).toFloat() * 1.dp.toPx()
                val x = size.width * (0.5f + dx) + sway
                val y = size.height * 0.46f - rise * t
                val heart = Path().apply {
                    moveTo(x, y + unit * .4f)
                    cubicTo(x - unit, y - unit * .4f, x - unit * .4f, y - unit * 1.1f, x, y - unit * .45f)
                    cubicTo(x + unit * .4f, y - unit * 1.1f, x + unit, y - unit * .4f, x, y + unit * .4f)
                    close()
                }
                drawPath(heart, rose.copy(alpha = (1f - t) * 0.95f))
            }
        }
    }
    }

}
