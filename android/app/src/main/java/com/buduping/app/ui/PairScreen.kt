package com.buduping.app.ui

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material3.Button
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.focus.FocusDirection
import androidx.compose.ui.input.key.Key
import androidx.compose.ui.input.key.KeyEventType
import androidx.compose.ui.input.key.key
import androidx.compose.ui.input.key.onKeyEvent
import androidx.compose.ui.input.key.type
import androidx.compose.ui.platform.LocalFocusManager
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.input.KeyboardCapitalization
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.buduping.app.PairCode

/**
 * Pairing screen: two 4-char boxes (XXXX-XXXX).
 * Auto-uppercase, auto-advance, checksum validated live; error shown immediately.
 */
@Composable
fun PairScreen(onPaired: (pairId: String) -> Unit) {
    var first by remember { mutableStateOf("") }
    var second by remember { mutableStateOf("") }
    val focusManager = LocalFocusManager.current

    val normalized = PairCode.normalize(first + second)
    val complete = normalized.length == 8
    val valid = complete && PairCode.isValid(first + second)
    val showError = complete && !valid

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(32.dp),
        verticalArrangement = Arrangement.Center,
        horizontalAlignment = Alignment.CenterHorizontally,
    ) {
        Text("🔗", fontSize = 56.sp)
        Spacer(Modifier.height(16.dp))
        Text(
            "Nhập mã cặp",
            style = MaterialTheme.typography.headlineSmall,
        )
        Spacer(Modifier.height(16.dp))
        Row(
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.Center,
        ) {
            CodeBox(
                value = first,
                onValueChange = { v ->
                    val n = PairCode.normalize(v)
                    if (n.length <= 4) {
                        first = n
                        if (n.length == 4) focusManager.moveFocus(FocusDirection.Next)
                    } else {
                        // pasted a full code into the first box: split it across both
                        first = n.take(4)
                        second = n.drop(4).take(4)
                        focusManager.moveFocus(FocusDirection.Next)
                    }
                },
            )
            Spacer(Modifier.width(8.dp))
            Text("-", fontSize = 28.sp, color = MaterialTheme.colorScheme.onSurfaceVariant)
            Spacer(Modifier.width(8.dp))
            CodeBox(
                value = second,
                onValueChange = { v -> second = PairCode.normalize(v).take(4) },
                onBackspaceEmpty = { focusManager.moveFocus(FocusDirection.Previous) },
            )
        }
        Spacer(Modifier.height(8.dp))
        if (showError) {
            Text(
                "Mã không đúng — check lại phía Windows nha",
                color = MaterialTheme.colorScheme.error,
                style = MaterialTheme.typography.bodyMedium,
            )
        } else {
            // keep the layout stable whether or not the error shows
            Text("", style = MaterialTheme.typography.bodyMedium)
        }
        Spacer(Modifier.height(16.dp))
        Button(
            onClick = { onPaired(normalized) },
            enabled = valid,
            modifier = Modifier.fillMaxWidth(),
        ) {
            Text("Kết nối")
        }
    }
}

@Composable
private fun CodeBox(
    value: String,
    onValueChange: (String) -> Unit,
    modifier: Modifier = Modifier,
    onBackspaceEmpty: (() -> Unit)? = null,
) {
    OutlinedTextField(
        value = value,
        onValueChange = onValueChange,
        modifier = modifier
            .width(104.dp)
            .onKeyEvent {
                if (it.type == KeyEventType.KeyDown &&
                    it.key == Key.Backspace &&
                    value.isEmpty()
                ) {
                    onBackspaceEmpty?.invoke()
                    true
                } else {
                    false
                }
            },
        textStyle = TextStyle(
            fontSize = 22.sp,
            textAlign = TextAlign.Center,
            letterSpacing = 4.sp,
        ),
        singleLine = true,
        keyboardOptions = KeyboardOptions(
            capitalization = KeyboardCapitalization.Characters,
            autoCorrect = false,
            keyboardType = KeyboardType.Text,
        ),
    )
}
