package com.buduping.app

import androidx.compose.ui.graphics.asAndroidBitmap
import org.junit.Assert.*
import android.graphics.Bitmap
import android.content.ContentValues
import android.provider.MediaStore
import androidx.compose.foundation.layout.*
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import androidx.compose.ui.test.*
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.test.platform.app.InstrumentationRegistry
import androidx.test.filters.SdkSuppress
import com.buduping.app.ui.*
import org.junit.Rule
import org.junit.Test
import java.io.File

@SdkSuppress(minSdkVersion = 29)
class HomeUiTest {
    @get:Rule val compose = createComposeRule()

    private fun capture(name: String) {
        compose.waitForIdle()
        val instrumentation = InstrumentationRegistry.getInstrumentation()
        val tag = when (name) {
            "character-picker" -> "characterPicker"
            "variant-picker" -> "variantPicker"
            "setup-light" -> "pairScreen"
            else -> "homeScreen"
        }
        val screenshot = compose.onNodeWithTag(tag, useUnmergedTree = true).captureToImage().asAndroidBitmap()
        // Public emulator media survives Gradle uninstalling the test target after the run.
        val resolver = instrumentation.targetContext.contentResolver
        val values = ContentValues().apply {
            put(MediaStore.Images.Media.DISPLAY_NAME, "$name.png")
            put(MediaStore.Images.Media.MIME_TYPE, "image/png")
            put(MediaStore.Images.Media.RELATIVE_PATH, "Pictures/BuduPingUI/")
        }
        val uri = requireNotNull(resolver.insert(MediaStore.Images.Media.getContentUri(MediaStore.VOLUME_EXTERNAL_PRIMARY), values))
        requireNotNull(resolver.openOutputStream(uri)).use { screenshot.compress(Bitmap.CompressFormat.PNG, 100, it) }
    }

    private fun show(dark: Boolean, scale: Float, width: Int = 360) {
        val context = InstrumentationRegistry.getInstrumentation().targetContext
        val prefs = Prefs(context).apply {
            myCharacter = CharacterKind.BUBU
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
            compose.onAllNodesWithTag("stickerArt", useUnmergedTree = true).fetchSemanticsNodes().size >= if (scale > 1.1f) 8 else 12
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

    @Test fun characterIdentityAndStrictFiltering() {
        val context = InstrumentationRegistry.getInstrumentation().targetContext
        val catalog = StickerCatalog.load(context)
        assertEquals(setOf("bubu"), catalog.first { it.id == "love-03" }.characters)
        assertEquals(setOf("dudu"), catalog.first { it.id == "love-02" }.characters)
        for (me in listOf("bubu", "dudu")) for (emotion in EMOTIONS) {
            val allowed = StickerCatalog.allowedFor(catalog, emotion.type, me)
            assertTrue(allowed.all { me in it.characters })
            repeat(10) {
                val pick = StickerCatalog.pickVariant(catalog, emotion.type, me)
                assertTrue(pick == null || me in pick.characters)
            }
        }
        assertTrue(StickerCatalog.allowedFor(catalog, "sad", "dudu").isEmpty())
        assertNull(StickerCatalog.pickVariant(catalog, "sad", "dudu"))
        show(false, 1f)
        compose.onNodeWithText("Bubu").performClick()
        compose.onNodeWithText("Gấu trắng").assertIsDisplayed()
        compose.onNodeWithText("Gấu nâu").assertIsDisplayed()
        capture("character-picker")
        compose.onNodeWithText("Dudu").performClick()
        compose.onNodeWithContentDescription("Buồn").assertIsNotEnabled()
        capture("home-dudu")
    }

    @Test fun identityMigrationPreservesAvatarOnce() {
        val context = InstrumentationRegistry.getInstrumentation().targetContext
        val raw = context.getSharedPreferences("buduping", android.content.Context.MODE_PRIVATE)
        raw.edit().putInt("character_schema", 1).putString("my_character", "bubu").commit()
        assertEquals("dudu", Prefs(context).myCharacter)
        assertEquals("dudu", Prefs(context).myCharacter)
        raw.edit().putInt("character_schema", 1).putString("my_character", "dudu").commit()
        assertEquals("bubu", Prefs(context).myCharacter)
    }

    @Test fun setupScreen() {
        compose.setContent { BuduPingTheme { PairScreen("", { _, _ -> }) } }
        compose.waitUntil(15_000) { compose.onAllNodesWithTag("stickerArt", useUnmergedTree = true).fetchSemanticsNodes().isNotEmpty() }
        compose.onNodeWithText("Kết nối").assertIsNotEnabled()
        capture("setup-light")
    }

    @Test fun darkHome() { show(true, 1f, 412); capture("home-dark-412") }
    @Test fun narrowLargeText() {
        show(false, 1.3f, 320)
        compose.onNodeWithText("Bubu").assertIsDisplayed()
        compose.onNodeWithText("Người thương · Chưa có mạng").assertDoesNotExist()
        capture("home-large-text-320")
    }
}
