package com.buduping.app

import android.content.Context
import android.graphics.Bitmap
import android.graphics.BitmapFactory
import android.util.LruCache
import org.json.JSONArray

data class StickerVariant(val id: String, val type: String, val file: String, val thumbnail: String,
                          val characters: Set<String> = emptySet())

/** Who the phone user plays: Bubu (white panda) or Dudu (brown bear). */
object CharacterKind {
    const val BUBU = "bubu"
    const val DUDU = "dudu"
}

object StickerCatalog {
    private var entries: List<StickerVariant>? = null
    private val thumbnails = object : LruCache<String, Bitmap>(4 * 1024 * 1024) {
        override fun sizeOf(key: String, value: Bitmap) = value.allocationByteCount
    }
    @Synchronized fun load(context: Context): List<StickerVariant> {
        entries?.let { return it }
        val array = JSONArray(context.assets.open("stickers/catalog.json").bufferedReader().use { it.readText() })
        return List(array.length()) { i ->
            val o = array.getJSONObject(i)
            val jsonChars = o.optJSONArray("characters")?.let { a ->
                List(a.length()) { a.getString(it) }.toSet()
            } ?: emptySet()
            StickerVariant(o.getString("id"), o.getString("type"), o.getString("file"),
                o.getString("thumbnail"), jsonChars)
        }.also { entries = it }
    }
    fun thumbnail(context: Context, variant: StickerVariant): Bitmap? {
        thumbnails.get(variant.id)?.let { return it }
        val bitmap = context.assets.open(variant.thumbnail).use { BitmapFactory.decodeStream(it) } ?: return null
        thumbnails.put(variant.id, bitmap)
        return bitmap
    }

    // The generated catalog is the single authority. Unknown assets fail closed.
    fun charactersOf(variant: StickerVariant): Set<String> = variant.characters

    fun allowedFor(catalog: List<StickerVariant>, type: String, me: String?): List<StickerVariant> =
        catalog.filter { it.type == type && (me.isNullOrBlank() || me in charactersOf(it)) }

    fun pickVariant(catalog: List<StickerVariant>, type: String, me: String?): StickerVariant? {
        val allowed = allowedFor(catalog, type, me)
        val solo = allowed.filter { charactersOf(it) == setOf(me) }
        return (solo.ifEmpty { allowed }).randomOrNull()
    }
}
