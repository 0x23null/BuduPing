package com.buduping.app.ui

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardActions
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.focus.FocusDirection
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalFocusManager
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardCapitalization
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.buduping.app.PairCode
import com.buduping.app.StickerCatalog
import com.buduping.app.StickerVariant
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

/** One small setup screen: name and pairing together, without separate wizard steps. */
@Composable
fun PairScreen(initialName: String, onPaired: (name: String, pairId: String) -> Unit) {
    val context = LocalContext.current
    val focus = LocalFocusManager.current
    var name by rememberSaveable { mutableStateOf(initialName) }
    var first by rememberSaveable { mutableStateOf("") }
    var second by rememberSaveable { mutableStateOf("") }
    val code = PairCode.normalize(first + second)
    val valid = code.length == 8 && PairCode.isValid(code)
    val error = code.length == 8 && !valid
    val art by produceState<StickerVariant?>(null) {
        value = withContext(Dispatchers.IO) { runCatching { StickerCatalog.load(context).firstOrNull { it.type == "hug" } }.getOrNull() }
    }
    fun connect() { if (valid && name.isNotBlank()) onPaired(name.trim(), code) }
    Surface(Modifier.fillMaxSize().testTag("pairScreen"), color = MaterialTheme.colorScheme.background) {
        Column(Modifier.fillMaxSize().windowInsetsPadding(WindowInsets.safeDrawing).imePadding()
            .verticalScroll(rememberScrollState()).padding(24.dp), horizontalAlignment = Alignment.CenterHorizontally) {
            Text("BuduPing", style = MaterialTheme.typography.headlineMedium, color = MaterialTheme.colorScheme.onBackground)
            StickerArt(art, Modifier.size(156.dp).padding(vertical = 12.dp), "Bubu và Dudu")
            Surface(shape = RoundedCornerShape(22.dp), color = MaterialTheme.colorScheme.surfaceVariant) {
                Column(Modifier.padding(20.dp)) {
                    Text("Tên của bạn", style = MaterialTheme.typography.titleSmall)
                    Spacer(Modifier.height(8.dp))
                    OutlinedTextField(value = name, onValueChange = { name = it.take(24) },
                        label = { Text("Người ấy gọi bạn là…") }, modifier = Modifier.fillMaxWidth(),
                        shape = RoundedCornerShape(14.dp), singleLine = true,
                        keyboardOptions = KeyboardOptions(capitalization = KeyboardCapitalization.Words, imeAction = ImeAction.Next))
                    Spacer(Modifier.height(24.dp))
                    Text("Ghép cặp với máy tính", style = MaterialTheme.typography.titleSmall)
                    Text("Mã nằm trong BuduPing trên PC.", style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant, modifier = Modifier.padding(top = 4.dp, bottom = 12.dp))
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        OutlinedTextField(value = first, onValueChange = {
                            val normalized = PairCode.normalize(it)
                            first = normalized.take(4)
                            if (normalized.length > 4) second = normalized.drop(4).take(4)
                            if (first.length == 4) focus.moveFocus(FocusDirection.Next)
                        }, modifier = Modifier.weight(1f), shape = RoundedCornerShape(14.dp),
                            textStyle = TextStyle(fontSize = 22.sp, letterSpacing = 2.sp, textAlign = TextAlign.Center),
                            placeholder = { Text("XXXX") }, singleLine = true, isError = error,
                            keyboardOptions = KeyboardOptions(capitalization = KeyboardCapitalization.Characters, autoCorrect = false, imeAction = ImeAction.Next))
                        Text("–", modifier = Modifier.padding(horizontal = 8.dp), color = MaterialTheme.colorScheme.primary)
                        OutlinedTextField(value = second, onValueChange = { second = PairCode.normalize(it).take(4) },
                            modifier = Modifier.weight(1f), shape = RoundedCornerShape(14.dp),
                            textStyle = TextStyle(fontSize = 22.sp, letterSpacing = 2.sp, textAlign = TextAlign.Center),
                            placeholder = { Text("XXXX") }, singleLine = true, isError = error,
                            keyboardOptions = KeyboardOptions(capitalization = KeyboardCapitalization.Characters, autoCorrect = false, imeAction = ImeAction.Done),
                            keyboardActions = KeyboardActions(onDone = { connect() }))
                    }
                    if (error) Text("Có ký tự chưa đúng. Kiểm tra lại mã trên PC nhé.", color = MaterialTheme.colorScheme.error,
                        style = MaterialTheme.typography.bodySmall, modifier = Modifier.padding(top = 8.dp))
                    Spacer(Modifier.height(20.dp))
                    Button(onClick = { connect() }, enabled = valid && name.isNotBlank(),
                        modifier = Modifier.fillMaxWidth().height(48.dp), shape = RoundedCornerShape(14.dp)) { Text("Kết nối") }
                }
            }
        }
    }
}
