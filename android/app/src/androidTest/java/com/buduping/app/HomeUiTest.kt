package com.buduping.app

import android.graphics.Bitmap
import androidx.compose.foundation.layout.*
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import androidx.compose.ui.test.*
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.test.platform.app.InstrumentationRegistry
import com.buduping.app.ui.*
import org.junit.Rule
import org.junit.Test
import java.io.File

class HomeUiTest {
    @get:Rule val compose = createComposeRule()

    private fun capture(name: String) {
        compose.waitForIdle()
        val instrumentation = InstrumentationRegistry.getInstrumentation()
        val dir = File(instrumentation.targetContext.getExternalFilesDir(null), "ui").apply { mkdirs() }
        val screenshot = requireNotNull(instrumentation.uiAutomation.takeScreenshot())
        File(dir, "$name.png").outputStream().use { screenshot.compress(Bitmap.CompressFormat.PNG, 100, it) }
    }

    private fun show(dark: Boolean, scale: Float, width: Int = 360) {
        val context = InstrumentationRegistry.getInstrumentation().targetContext
        val prefs = Prefs(context).apply {
            senderName = "Người thương"; pairId = "2HBRNKWT"; favorites = emptyList(); pendingKey = ""; deliveryStatus = ""
        }
        compose.setContent {
            val density = LocalDensity.current.density
            CompositionLocalProvider(LocalDensity provides Density(density, scale)) {
                BuduPingTheme(darkTheme = dark) {
                    Box(Modifier.width(width.dp).fillMaxHeight()) {
                        // No observers or sends: these tests never write to the production database.
                        HomeScreen(prefs, FirebaseRepo(), false, {}, {})
                    }
                }
            }
        }
        compose.waitUntil(15_000) {
            compose.onAllNodesWithTag("stickerArt", useUnmergedTree = true).fetchSemanticsNodes().size >= 4
        }
        compose.onAllNodesWithText("Gợi ý", substring = true).assertCountEquals(0)
    }

    @Test fun lightHomeAndVariantPicker() {
        show(false, 1f)
        capture("home-light-360")
        compose.onNodeWithContentDescription("Ôm").performTouchInput { longClick() }
        compose.onNodeWithText("Gửi sticker này").assertIsDisplayed()
        capture("variant-picker")
    }

    @Test fun setupScreen() {
        compose.setContent { BuduPingTheme { PairScreen("", { _, _ -> }) } }
        compose.waitUntil(15_000) { compose.onAllNodesWithTag("stickerArt", useUnmergedTree = true).fetchSemanticsNodes().isNotEmpty() }
        compose.onNodeWithText("Kết nối").assertIsNotEnabled()
        capture("setup-light")
    }

    @Test fun darkHome() { show(true, 1f, 412); capture("home-dark-412") }
    @Test fun narrowLargeText() { show(false, 1.3f, 320); capture("home-large-text-320") }
}
