package com.buduping.app.ui

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.unit.dp
import com.buduping.app.CharacterKind
import com.buduping.app.StickerVariant

private data class CharacterChoice(val kind: String, val name: String, val species: String, val previewId: String)

private val CHOICES = listOf(
    CharacterChoice(CharacterKind.BUBU, "Bubu", "Gấu trắng", "love-03"),
    CharacterChoice(CharacterKind.DUDU, "Dudu", "Gấu nâu", "funny-03"),
)

/** Preview used for a character's chip/avatar in the header. */
fun characterPreview(catalog: List<StickerVariant>, kind: String?): StickerVariant? =
    CHOICES.firstOrNull { it.kind == kind }?.let { choice ->
        catalog.firstOrNull { it.id == choice.previewId }
    }

fun characterName(kind: String?): String = when (kind) {
    CharacterKind.DUDU -> "Dudu"
    CharacterKind.BUBU -> "Bubu"
    else -> ""
}

/**
 * One-time "who are you" picker: two big cards with live sticker previews.
 * Reused as a dialog when the user taps their character chip to switch.
 */
@Composable
fun CharacterPicker(catalog: List<StickerVariant>, onPick: (String) -> Unit, onDismiss: () -> Unit) {
    AlertDialog(modifier = Modifier.testTag("characterPicker"), onDismissRequest = onDismiss, title = { Text("Bạn là ai trong hai đứa?", fontFamily = BalooFamily) }, text = {
        Column {
            Text("Từ giờ sticker bạn gửi sẽ là của nhân vật bạn chọn.",
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant)
            Spacer(Modifier.height(12.dp))
            Row(horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                CHOICES.forEach { choice ->
                    Surface(shape = RoundedCornerShape(16.dp), color = MaterialTheme.colorScheme.surfaceVariant,
                        modifier = Modifier.weight(1f).clickable { onPick(choice.kind) }) {
                        Column(Modifier.padding(12.dp), horizontalAlignment = Alignment.CenterHorizontally) {
                            StickerArt(characterPreview(catalog, choice.kind), Modifier.size(96.dp), choice.name)
                            Spacer(Modifier.height(8.dp))
                            Text(choice.name, style = MaterialTheme.typography.titleMedium)
                            Text(choice.species, style = MaterialTheme.typography.labelSmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant)
                        }
                    }
                }
            }
        }
    }, confirmButton = {}, dismissButton = { TextButton(onClick = onDismiss) { Text("Để sau") } })
}
