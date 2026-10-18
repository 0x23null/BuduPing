package com.buduping.app.ui

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

private val Day = lightColorScheme(
    primary = Color(0xFFA93F65), onPrimary = Color.White,
    primaryContainer = Color(0xFFF6DDE6), onPrimaryContainer = Color(0xFF54233A),
    secondary = Color(0xFF765342), onSecondary = Color.White,
    secondaryContainer = Color(0xFFF5E5DB), onSecondaryContainer = Color(0xFF51382B),
    background = Color(0xFFFFF7FA), onBackground = Color(0xFF382923),
    surface = Color(0xFFFFF7FA), onSurface = Color(0xFF382923),
    surfaceVariant = Color(0xFFFFFFFF), onSurfaceVariant = Color(0xFF746057),
    outline = Color(0xFF91796F), outlineVariant = Color(0xFFE5D6CE),
)
private val Night = darkColorScheme(
    primary = Color(0xFFFFAEC9), onPrimary = Color(0xFF542138),
    primaryContainer = Color(0xFF663349), onPrimaryContainer = Color(0xFFFFD9E5),
    secondary = Color(0xFFE5BDA7), onSecondary = Color(0xFF432C21),
    secondaryContainer = Color(0xFF51382B), onSecondaryContainer = Color(0xFFF5E5DB),
    background = Color(0xFF211B18), onBackground = Color(0xFFF7E8DF),
    surface = Color(0xFF211B18), onSurface = Color(0xFFF7E8DF),
    surfaceVariant = Color(0xFF342923), onSurfaceVariant = Color(0xFFD4BEB1),
    outline = Color(0xFFAA9184), outlineVariant = Color(0xFF57463D),
)

@Composable
fun BuduPingTheme(darkTheme: Boolean = isSystemInDarkTheme(), content: @Composable () -> Unit) {
    MaterialTheme(colorScheme = if (darkTheme) Night else Day, content = content)
}
