package com.buduping.app.ui

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

private val BuduPingColors = lightColorScheme(
    primary = Color(0xFFE0527E),
    onPrimary = Color.White,
    primaryContainer = Color(0xFFFFD9E4),
    onPrimaryContainer = Color(0xFF6B1B33),
    secondary = Color(0xFF8A5A6B),
    background = Color(0xFFFFF7F9),
    surface = Color.White,
    surfaceVariant = Color(0xFFFFE8EF),
    onSurfaceVariant = Color(0xFF6B4A55),
    error = Color(0xFFBA1A1A),
)

@Composable
fun BuduPingTheme(content: @Composable () -> Unit) {
    MaterialTheme(
        colorScheme = BuduPingColors,
        content = content,
    )
}
