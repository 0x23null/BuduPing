package com.buduping.app

import android.content.Context
import android.graphics.Bitmap
import android.graphics.BitmapFactory
import android.util.LruCache
import org.json.JSONArray

data class StickerVariant(val id: String, val type: String, val file: String, val thumbnail: String,
                          val characters: Set<String> = emptySet())

/** Who the phone user plays: Bubu (brown bear) or Dudu (white panda). */
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

    /**
     * Embedded per-variant character audit, used when a catalog entry has no
     * "characters" field yet (the generator adds it separately). Kept in sync
     * with the curated sticker set on the Windows side.
     */
    private val FALLBACK_CHARACTERS = mapOf(
        "angry-01" to setOf(CharacterKind.BUBU, CharacterKind.DUDU),
        "angry-02" to setOf(CharacterKind.BUBU),
        "cheer-01" to setOf(CharacterKind.BUBU, CharacterKind.DUDU),
        "cheer-03" to setOf(CharacterKind.BUBU, CharacterKind.DUDU),
        "funny-01" to setOf(CharacterKind.DUDU),
        "funny-02" to setOf(CharacterKind.BUBU, CharacterKind.DUDU),
        "funny-03" to setOf(CharacterKind.BUBU),
        "goodnight-01" to setOf(CharacterKind.BUBU, CharacterKind.DUDU),
        "goodnight-03" to setOf(CharacterKind.BUBU, CharacterKind.DUDU),
        "hug-02" to setOf(CharacterKind.BUBU, CharacterKind.DUDU),
        "hug-03" to setOf(CharacterKind.BUBU, CharacterKind.DUDU),
        "hug-04" to setOf(CharacterKind.BUBU, CharacterKind.DUDU),
        "hungry-01" to setOf(CharacterKind.BUBU, CharacterKind.DUDU),
        "hungry-02" to setOf(CharacterKind.DUDU),
        "kiss-01" to setOf(CharacterKind.BUBU, CharacterKind.DUDU),
        "kiss-02" to setOf(CharacterKind.BUBU, CharacterKind.DUDU),
        "kiss-03" to setOf(CharacterKind.BUBU, CharacterKind.DUDU),
        "love-01" to setOf(CharacterKind.BUBU, CharacterKind.DUDU),
        "love-02" to setOf(CharacterKind.BUBU),
        "love-03" to setOf(CharacterKind.DUDU),
        "miss-01" to setOf(CharacterKind.BUBU, CharacterKind.DUDU),
        "miss-03" to setOf(CharacterKind.BUBU, CharacterKind.DUDU),
        "miss-04" to setOf(CharacterKind.DUDU),
        "sad-01" to setOf(CharacterKind.DUDU),
        "sad-02" to setOf(CharacterKind.DUDU),
        "sorry-01" to setOf(CharacterKind.DUDU),
        "sorry-02" to setOf(CharacterKind.DUDU),
        "tease-02" to setOf(CharacterKind.BUBU, CharacterKind.DUDU),
    )

    /**
     * Character set for a variant. The catalog's "characters" field wins when
     * present; otherwise the embedded audit map; an unknown id is treated as
     * both characters so it is never wrongly excluded from someone's sticker.
     */
    fun charactersOf(variant: StickerVariant): Set<String> =
        variant.characters.ifEmpty {
            FALLBACK_CHARACTERS[variant.id] ?: setOf(CharacterKind.BUBU, CharacterKind.DUDU)
        }

    /**
     * Variants of [type] this user is allowed to send: their character's solos
     * plus duo stickers. Never includes the other character's solos. If nothing
     * is allowed (should not happen), falls back to all variants of the type
     * so the picker never shows empty.
     */
    fun allowedFor(catalog: List<StickerVariant>, type: String, me: String?): List<StickerVariant> {
        val ofType = catalog.filter { it.type == type }
        if (me.isNullOrBlank()) return ofType
        val allowed = ofType.filter { me in charactersOf(it) }
        return allowed.ifEmpty { ofType }
    }

    /**
     * Variant to send on a quick tap: prefer a solo sticker of my character
     * (random), then a duo sticker (random), then — as a last resort — any
     * variant of the type. Always non-null when the type exists in the catalog.
     */
    fun pickVariant(catalog: List<StickerVariant>, type: String, me: String?): StickerVariant? {
        val ofType = catalog.filter { it.type == type }
        if (ofType.isEmpty() || me.isNullOrBlank()) return ofType.randomOrNull()
        val mine = ofType.filter { charactersOf(it) == setOf(me) }
        val duo = ofType.filter { charactersOf(it).size > 1 }
        return (mine.ifEmpty { duo }.ifEmpty { ofType }).random()
    }
}
