package com.buduping.app

/** Process-wide foreground flag, kept by MainActivity; ReplyService reads it
 *  so a hug-back becomes the in-app dialog when the user is looking at the
 *  app, and a system notification when they are not. */
object AppForeground {
    @Volatile var isForeground: Boolean = false
}
