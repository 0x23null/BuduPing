package com.buduping.app.ui

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.buduping.app.PairCode
import com.buduping.app.Prefs

/** Light settings: rename sender, view/change pair code, about. */
@Composable
fun SettingsDialog(
    prefs: Prefs,
    onDismiss: () -> Unit,
    onChangeCode: () -> Unit,
) {
    var name by remember { mutableStateOf(prefs.senderName) }
    val trimmed = name.trim()

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Cài đặt") },
        text = {
            Column {
                Text("Tên người gửi", style = MaterialTheme.typography.labelLarge)
                Spacer(Modifier.height(4.dp))
                OutlinedTextField(
                    value = name,
                    onValueChange = { if (it.length <= 24) name = it },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth(),
                )
                Spacer(Modifier.height(8.dp))
                Button(
                    onClick = {
                        prefs.senderName = trimmed
                        onDismiss()
                    },
                    enabled = trimmed.isNotEmpty(),
                ) {
                    Text("Lưu tên")
                }
                Spacer(Modifier.height(16.dp))
                Text("Mã cặp", style = MaterialTheme.typography.labelLarge)
                Spacer(Modifier.height(4.dp))
                Text(
                    PairCode.format(prefs.pairId),
                    style = MaterialTheme.typography.titleMedium,
                )
                Text(
                    "Đổi mã sẽ ngắt kết nối hiện tại.",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
                Spacer(Modifier.height(8.dp))
                OutlinedButton(onClick = onChangeCode) {
                    Text("Đổi mã cặp")
                }
                Spacer(Modifier.height(16.dp))
                Text(
                    "BuduPing 0.2.0",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
        },
        confirmButton = {
            TextButton(onClick = onDismiss) { Text("Đóng") }
        },
    )
}
