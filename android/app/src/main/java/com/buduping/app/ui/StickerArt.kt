package com.buduping.app.ui

import android.graphics.ImageDecoder
import android.graphics.drawable.AnimatedImageDrawable
import android.graphics.drawable.Drawable
import android.os.Build
import android.widget.ImageView
import androidx.compose.foundation.Image
import androidx.compose.foundation.layout.Box
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.asImageBitmap
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.viewinterop.AndroidView
import com.buduping.app.StickerCatalog
import com.buduping.app.StickerVariant
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

@Composable
fun StickerArt(variant: StickerVariant?, modifier: Modifier = Modifier, description: String? = null, animated: Boolean = false) {
    val context = LocalContext.current
    val thumbnail by produceState<android.graphics.Bitmap?>(null, variant?.id) {
        value = withContext(Dispatchers.IO) { variant?.let { StickerCatalog.thumbnail(context, it) } }
    }
    val drawable by produceState<Drawable?>(null, variant?.id, animated) {
        if (animated && variant != null && Build.VERSION.SDK_INT >= 28) {
            value = withContext(Dispatchers.IO) {
                runCatching {
                    ImageDecoder.decodeDrawable(ImageDecoder.createSource(context.assets, "stickers/${variant.file}"))
                }.getOrNull()
            }
        }
    }
    DisposableEffect(drawable) {
        onDispose { if (Build.VERSION.SDK_INT >= 28) (drawable as? AnimatedImageDrawable)?.stop() }
    }
    if (drawable != null) {
        AndroidView(modifier = modifier, factory = { ImageView(it).apply { scaleType = ImageView.ScaleType.FIT_CENTER } }, update = {
            if (it.drawable !== drawable) it.setImageDrawable(drawable)
            if (Build.VERSION.SDK_INT >= 28) (drawable as? AnimatedImageDrawable)?.start()
            it.contentDescription = description
        })
    } else if (thumbnail != null) {
        Image(bitmap = thumbnail!!.asImageBitmap(), contentDescription = description, modifier = modifier.testTag("stickerArt"), contentScale = ContentScale.Fit)
    } else Box(modifier)
}
