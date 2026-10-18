package com.buduping.app.ui

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.semantics.*
import androidx.compose.ui.unit.dp
import com.buduping.app.Emotion
import com.buduping.app.StickerVariant

@Composable
fun VariantPicker(emotion: Emotion, variants: List<StickerVariant>, favorite: Boolean,
                  onFavorite: () -> Unit, onDismiss: () -> Unit, onSend: (StickerVariant) -> Unit) {
    var selected by remember(emotion.type) { mutableStateOf(variants.firstOrNull()) }
    AlertDialog(onDismissRequest = onDismiss, title = { Text(emotion.label) }, text = {
        Column {
            StickerArt(selected, Modifier.fillMaxWidth().height(180.dp), emotion.label, animated = true)
            LazyRow(horizontalArrangement = Arrangement.spacedBy(8.dp), contentPadding = PaddingValues(vertical = 12.dp)) {
                items(variants, key = { it.id }) { variant ->
                    Surface(shape = RoundedCornerShape(12.dp),
                        color = if (variant == selected) MaterialTheme.colorScheme.primaryContainer else MaterialTheme.colorScheme.surfaceVariant,
                        modifier = Modifier.size(72.dp).clickable { selected = variant }.semantics {
                            contentDescription = "Biến thể ${variant.id}"; this.selected = variant == selected; role = Role.Button
                        }) {
                        StickerArt(variant, Modifier.padding(4.dp))
                    }
                }
            }
            TextButton(onClick = onFavorite) { Text(if (favorite) "Bỏ ghim cảm xúc" else "Ghim lên đầu") }
        }
    }, confirmButton = {
        Button(onClick = { selected?.let(onSend) }, enabled = selected != null) { Text("Gửi sticker này") }
    }, dismissButton = { TextButton(onClick = onDismiss) { Text("Đóng") } })
}
