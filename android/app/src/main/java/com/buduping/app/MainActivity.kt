package com.buduping.app

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import com.buduping.app.ui.BuduPingTheme
import com.buduping.app.ui.HomeScreen
import com.buduping.app.ui.PairScreen

class MainActivity : ComponentActivity() {
    private var foreground by mutableStateOf(false)
    override fun onStart() { super.onStart(); foreground = true }
    override fun onStop() { foreground = false; super.onStop() }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        val prefs = Prefs(this)
        val repo = FirebaseRepo()

        setContent {
            BuduPingTheme {
                var screen by remember { mutableStateOf(initialScreen(prefs)) }
                when (screen) {
                    Screen.Name, Screen.Pair -> PairScreen(
                        initialName = prefs.senderName,
                        onPaired = { name, pairId ->
                            prefs.senderName = name
                            prefs.pairId = pairId
                            screen = Screen.Home
                        },
                    )

                    Screen.Home -> HomeScreen(
                        prefs = prefs,
                        repo = repo,
                        isForeground = foreground,
                        onUnpair = {
                            prefs.clearPair()
                            screen = Screen.Pair
                        },
                        onChangeCode = {
                            prefs.clearPair()
                            screen = Screen.Pair
                        },
                    )
                }
            }
        }
    }

    private fun initialScreen(prefs: Prefs): Screen =
        when {
            prefs.senderName.isBlank() -> Screen.Name
            prefs.pairId.isBlank() -> Screen.Pair
            else -> Screen.Home
        }
}

private sealed interface Screen {
    data object Name : Screen
    data object Pair : Screen
    data object Home : Screen
}
