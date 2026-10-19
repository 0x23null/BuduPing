package com.buduping.app

data class Emotion(val type: String, val label: String, val emoji: String)

/** The emotions on the home grid: hello (edge-peek greeting) + the 12 core ones. */
val EMOTIONS = listOf(
    Emotion("hello", "Chào", "👋"),
    Emotion("miss", "Nhớ", "🥺"),
    Emotion("love", "Yêu", "❤️"),
    Emotion("hug", "Ôm", "🤗"),
    Emotion("kiss", "Hôn", "😘"),
    Emotion("tease", "Ghét", "😤"),
    Emotion("angry", "Dỗi", "😠"),
    Emotion("sad", "Buồn", "🥲"),
    Emotion("sorry", "Xin lỗi", "🥹"),
    Emotion("funny", "Cười", "😂"),
    Emotion("cheer", "Cố lên", "💪"),
    Emotion("hungry", "Đói", "🍜"),
    Emotion("goodnight", "Ngủ ngon", "🌙"),
    Emotion("dance", "Nhảy", "💃"),
    Emotion("wow", "Wow", "😲"),
    Emotion("shy", "Ngại", "🙈"),
    Emotion("ok", "Ok", "👌"),
    Emotion("proud", "Kiêu", "😎"),
    Emotion("sleepy", "Buồn ngủ", "🥱"),
)

/** Short labels used by the daily fun counter. */
val FUN_LABELS = mapOf(
    "hello" to "chào",
    "miss" to "nhớ",
    "love" to "yêu",
    "hug" to "ôm",
    "kiss" to "hôn",
    "tease" to "ghét",
    "angry" to "dỗi",
    "sad" to "buồn",
    "sorry" to "xin lỗi",
    "funny" to "cười",
    "cheer" to "cố lên",
    "hungry" to "đói",
    "goodnight" to "ngủ ngon",
    "dance" to "nhảy",
    "wow" to "wow",
    "shy" to "ngại",
    "ok" to "ok",
    "proud" to "kiêu",
    "sleepy" to "buồn ngủ",
)

/** Short one-tap preset message chips. */
val PRESET_CHIPS = listOf(
    "nhớ quá đi 🥺",
    "ăn cơm chưa?",
    "ôm cái nè 🤗",
    "ngủ ngon nha 🌙",
)
