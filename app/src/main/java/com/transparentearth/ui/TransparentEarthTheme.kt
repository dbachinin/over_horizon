package com.transparentearth.ui

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Typography
import androidx.compose.material3.darkColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.sp

private val EarthColors = darkColorScheme(
    primary = Color(0xFF9DF6D2), onPrimary = Color(0xFF07110F), secondary = Color(0xFFFFD66D),
    background = Color(0xFF07110F), onBackground = Color(0xFFF1F8F5), surface = Color(0xFF12211D),
    onSurface = Color(0xFFF1F8F5), outline = Color(0xFF355047)
)

private val EarthTypography = Typography(
    headlineMedium = TextStyle(
        fontFamily = FontFamily.SansSerif, fontWeight = FontWeight.Light, fontSize = 30.sp,
        lineHeight = 35.sp, letterSpacing = (-0.5).sp
    ),
    headlineSmall = TextStyle(
        fontFamily = FontFamily.SansSerif, fontWeight = FontWeight.Medium, fontSize = 24.sp, lineHeight = 29.sp
    ),
    titleLarge = TextStyle(
        fontFamily = FontFamily.SansSerif, fontWeight = FontWeight.Medium, fontSize = 19.sp,
        lineHeight = 24.sp, letterSpacing = (-0.2).sp
    ),
    bodyMedium = TextStyle(
        fontFamily = FontFamily.SansSerif, fontWeight = FontWeight.Normal, fontSize = 14.sp, lineHeight = 20.sp
    )
)

@Composable
fun TransparentEarthTheme(content: @Composable () -> Unit) {
    MaterialTheme(colorScheme = EarthColors, typography = EarthTypography, content = content)
}
