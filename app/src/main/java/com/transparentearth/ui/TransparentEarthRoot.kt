package com.transparentearth.ui

import androidx.compose.animation.AnimatedContent
import androidx.compose.animation.animateColorAsState
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.*
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.hilt.navigation.compose.hiltViewModel
import com.transparentearth.data.local.CityEntity
import com.transparentearth.geo.GeoMath
import com.transparentearth.geo.GeoPoint
import kotlin.math.abs
import kotlin.math.roundToInt

private val Ink = Color(0xFF07110F)
private val Panel = Color(0xE612211D)
private val Line = Color(0xFF29463E)
private val Mint = Color(0xFF9DF6D2)
private val Signal = Color(0xFFFFD66D)
private val Muted = Color(0xFF91A79F)

@Composable
fun TransparentEarthRoot(viewModel: ExploreViewModel = hiltViewModel()) {
    val state by viewModel.state.collectAsState()
    var tab by remember { mutableIntStateOf(0) }

    TransparentEarthTheme {
        Scaffold(
            containerColor = Color.Transparent,
            bottomBar = { EarthNavigation(tab, onSelect = { tab = it }) }
        ) { padding ->
            Box(
                Modifier
                    .fillMaxSize()
                    .background(Brush.verticalGradient(listOf(Color(0xFF081511), Ink, Color(0xFF050A09))))
                    .padding(padding)
            ) {
                AnimatedContent(targetState = tab, label = "section") { selected ->
                    when (selected) {
                        0 -> ExploreScreen(state, viewModel::setTransparent, viewModel::setFilter)
                        1 -> AntipodeScreen(state.location, viewModel.antipode())
                        else -> PlaceholderScreen(
                            title = listOf("Карта мира", "Сохранённые места", "Профиль")[selected - 2],
                            symbol = listOf("⌁", "◇", "○")[selected - 2]
                        )
                    }
                }
            }
        }
    }
}

@Composable
private fun ExploreScreen(
    state: ExploreState,
    onTransparent: (Boolean) -> Unit,
    onFilter: (DistanceFilter) -> Unit
) {
    Column(Modifier.fillMaxSize()) {
        InstrumentHeader()
        EarthViewport(state, Modifier.weight(1f))
        ControlDeck(state, onTransparent, onFilter)
    }
}

@Composable
private fun InstrumentHeader() {
    Row(
        Modifier.fillMaxWidth().padding(horizontal = 20.dp, vertical = 13.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Column(Modifier.weight(1f)) {
            Text("TRANSPARENT EARTH", color = Mint, fontSize = 11.sp, fontWeight = FontWeight.Bold, letterSpacing = 2.2.sp)
            Spacer(Modifier.height(3.dp))
            Text("Смотрите сквозь горизонт", style = MaterialTheme.typography.titleLarge)
        }
        Row(
            Modifier.clip(RoundedCornerShape(50)).background(Color(0xFF132620))
                .border(1.dp, Line, RoundedCornerShape(50)).padding(horizontal = 11.dp, vertical = 7.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Box(Modifier.size(6.dp).background(Mint, CircleShape))
            Spacer(Modifier.width(7.dp))
            Text("LIVE", color = Mint, fontSize = 10.sp, fontWeight = FontWeight.Bold, letterSpacing = 1.4.sp)
        }
    }
}

@Composable
private fun EarthViewport(state: ExploreState, modifier: Modifier = Modifier) {
    BoxWithConstraints(modifier.fillMaxWidth()) {
        val width = maxWidth
        val height = maxHeight
        EarthField(state, Modifier.fillMaxSize())

        Row(
            Modifier.align(Alignment.TopCenter).padding(top = 8.dp).clip(RoundedCornerShape(8.dp))
                .background(Color(0xA6081210)).border(1.dp, Color(0x334C7B6D), RoundedCornerShape(8.dp))
                .padding(horizontal = 10.dp, vertical = 6.dp),
            horizontalArrangement = Arrangement.spacedBy(14.dp)
        ) {
            Metric("AZ", "${normalizeDegrees(state.heading).roundToInt()}°")
            Metric("TILT", signed(state.pitch))
            Metric("GPS", "±4 m", Mint)
        }

        val visibleCities = remember(state.cities, state.heading, state.pitch) {
            state.cities
                .map { it to GeoMath.project(state.location, GeoPoint(it.latitude, it.longitude)) }
                .sortedBy { it.second.distanceKm }
                .take(if (state.transparentEarth) 5 else 3)
        }
        visibleCities.forEachIndexed { index, (city, projection) ->
            val bearingOffset = shortestAngle(projection.bearingDeg.toFloat() - state.heading)
            val xFraction = (0.5f + bearingOffset / 150f).coerceIn(0.07f, 0.70f)
            val depth = (abs(projection.verticalAngleDeg) / 90.0).coerceIn(0.0, 1.0).toFloat()
            val yFraction = if (index == 0 && projection.distanceKm < 1) 0.30f else 0.40f + depth * 0.32f
            CityMarker(
                city = city,
                distanceKm = projection.distanceKm,
                belowHorizon = projection.verticalAngleDeg,
                isDeep = projection.verticalAngleDeg < -35,
                modifier = Modifier.offset(
                    x = width * xFraction,
                    y = (height * yFraction + (index % 2 * 18).dp).coerceAtMost(height - 76.dp)
                )
            )
        }

        Text(
            "ФИЗИЧЕСКИЙ ГОРИЗОНТ",
            modifier = Modifier.align(Alignment.CenterStart).padding(start = 20.dp).offset(y = 4.dp),
            color = Color(0xFF769087), fontSize = 8.sp, fontWeight = FontWeight.Bold, letterSpacing = 1.5.sp
        )
        Text(
            if (state.transparentEarth) "ПРОЗРАЧНЫЙ СЛОЙ · АКТИВЕН" else "ПОВЕРХНОСТНЫЙ СЛОЙ",
            modifier = Modifier.align(Alignment.BottomCenter).padding(bottom = 16.dp),
            color = if (state.transparentEarth) Signal else Muted,
            fontSize = 9.sp, fontWeight = FontWeight.Bold, letterSpacing = 1.6.sp
        )
    }
}

@Composable
private fun EarthField(state: ExploreState, modifier: Modifier = Modifier) {
    val transparentAlpha by animateFloatAsState(if (state.transparentEarth) 1f else .34f, label = "earthAlpha")
    Canvas(modifier) {
        drawRect(
            Brush.radialGradient(
                colors = listOf(Color(0x203FE3AA), Color.Transparent),
                center = Offset(size.width * .52f, size.height * .45f),
                radius = size.minDimension * .8f
            )
        )
        val horizonY = size.height * .5f
        val earthRect = Rect(-size.width * .42f, horizonY - size.width * .05f, size.width * 1.42f, horizonY + size.width * 1.79f)
        drawArc(
            brush = Brush.verticalGradient(listOf(Color(0xCC163B31), Color(0xFF07100E)), horizonY, size.height),
            startAngle = 180f, sweepAngle = 180f, useCenter = true,
            topLeft = earthRect.topLeft, size = earthRect.size
        )
        drawArc(
            color = Mint.copy(alpha = .7f), startAngle = 184f, sweepAngle = 172f,
            useCenter = false, topLeft = earthRect.topLeft, size = earthRect.size,
            style = Stroke(width = 1.4.dp.toPx())
        )
        repeat(5) { ring ->
            val inset = ring * size.width * .075f
            val ringRect = Rect(
                earthRect.left + inset, earthRect.top + ring * size.height * .11f,
                earthRect.right - inset, earthRect.bottom - ring * size.height * .04f
            )
            drawArc(
                color = Mint.copy(alpha = (.13f - ring * .014f) * transparentAlpha),
                startAngle = 190f, sweepAngle = 160f, useCenter = false,
                topLeft = ringRect.topLeft, size = ringRect.size, style = Stroke(1.dp.toPx())
            )
        }
        repeat(7) { line ->
            val x = size.width * (line + 1) / 8f
            drawLine(
                Color(0xFF78DDB9).copy(alpha = .09f * transparentAlpha),
                Offset(size.width / 2f, horizonY), Offset(x, size.height), 1.dp.toPx()
            )
        }
        drawLine(Color(0x555D8177), Offset(0f, horizonY), Offset(size.width, horizonY), 1.dp.toPx())
        repeat(15) { tick ->
            val x = size.width * tick / 14f
            val long = tick % 2 == 0
            drawLine(
                Color(0x9978948B), Offset(x, horizonY - if (long) 5.dp.toPx() else 2.dp.toPx()),
                Offset(x, horizonY + if (long) 5.dp.toPx() else 2.dp.toPx()), 1.dp.toPx()
            )
        }
        val center = Offset(size.width / 2, horizonY)
        drawCircle(Color(0x6685D9BB), 20.dp.toPx(), center, style = Stroke(1.dp.toPx(), pathEffect = PathEffect.dashPathEffect(floatArrayOf(5f, 5f))))
        drawCircle(Mint, 2.5.dp.toPx(), center)
        drawLine(Mint.copy(alpha = .45f), center - Offset(29.dp.toPx(), 0f), center - Offset(9.dp.toPx(), 0f), 1.dp.toPx())
        drawLine(Mint.copy(alpha = .45f), center + Offset(9.dp.toPx(), 0f), center + Offset(29.dp.toPx(), 0f), 1.dp.toPx())
    }
}

@Composable
private fun CityMarker(city: CityEntity, distanceKm: Double, belowHorizon: Double, isDeep: Boolean, modifier: Modifier = Modifier) {
    Row(modifier, verticalAlignment = Alignment.CenterVertically) {
        Box(contentAlignment = Alignment.Center) {
            Box(Modifier.size(if (isDeep) 9.dp else 11.dp).border(1.dp, if (isDeep) Signal else Mint, CircleShape))
            Box(Modifier.size(3.dp).background(if (isDeep) Signal else Mint, CircleShape))
        }
        Spacer(Modifier.width(8.dp))
        Column(
            Modifier.clip(RoundedCornerShape(8.dp)).background(Color(0xC9081411))
                .border(1.dp, if (isDeep) Color(0x55FFD66D) else Color(0x443D8C73), RoundedCornerShape(8.dp))
                .padding(horizontal = 9.dp, vertical = 6.dp)
        ) {
            Text(city.name.uppercase(), color = Color.White, fontSize = 10.sp, fontWeight = FontWeight.Bold, letterSpacing = .8.sp)
            Text(
                "${formatDistance(distanceKm)}  ·  ${if (belowHorizon < -.1) "${abs(belowHorizon).format1()}° НИЖЕ" else "НА ГОРИЗОНТЕ"}",
                color = if (isDeep) Signal else Mint, fontSize = 8.sp, fontWeight = FontWeight.Medium, letterSpacing = .4.sp
            )
        }
    }
}

@Composable
private fun ControlDeck(state: ExploreState, onTransparent: (Boolean) -> Unit, onFilter: (DistanceFilter) -> Unit) {
    Column(
        Modifier.padding(horizontal = 14.dp, vertical = 8.dp).clip(RoundedCornerShape(24.dp)).background(Panel)
            .border(1.dp, Line, RoundedCornerShape(24.dp)).padding(vertical = 12.dp)
    ) {
        Row(Modifier.fillMaxWidth().padding(horizontal = 16.dp), verticalAlignment = Alignment.CenterVertically) {
            Box(Modifier.size(36.dp).clip(CircleShape).background(Color(0xFF1D352E)), contentAlignment = Alignment.Center) {
                Text("◉", color = Mint, fontSize = 17.sp)
            }
            Spacer(Modifier.width(11.dp))
            Column(Modifier.weight(1f)) {
                Text("Прозрачная Земля", fontWeight = FontWeight.SemiBold, fontSize = 15.sp)
                Text("Объекты за линией горизонта", color = Muted, fontSize = 11.sp)
            }
            Switch(
                checked = state.transparentEarth, onCheckedChange = onTransparent,
                colors = SwitchDefaults.colors(
                    checkedThumbColor = Ink, checkedTrackColor = Mint, uncheckedThumbColor = Muted,
                    uncheckedTrackColor = Color(0xFF22312D), uncheckedBorderColor = Line
                )
            )
        }
        Spacer(Modifier.height(12.dp))
        Row(
            Modifier.fillMaxWidth().horizontalScroll(rememberScrollState()).padding(horizontal = 12.dp),
            horizontalArrangement = Arrangement.spacedBy(7.dp)
        ) {
            DistanceFilter.entries.forEach { filter ->
                FilterPill(filterLabel(filter), state.filter == filter) { onFilter(filter) }
            }
        }
    }
}

@Composable
private fun FilterPill(label: String, selected: Boolean, onClick: () -> Unit) {
    val background by animateColorAsState(if (selected) Mint else Color.Transparent, label = "filterColor")
    Text(
        label,
        modifier = Modifier.clip(RoundedCornerShape(50)).background(background)
            .border(1.dp, if (selected) Mint else Line, RoundedCornerShape(50)).clickable(onClick = onClick)
            .padding(horizontal = 13.dp, vertical = 8.dp),
        color = if (selected) Ink else Muted, fontSize = 11.sp, fontWeight = FontWeight.SemiBold
    )
}

@Composable
private fun AntipodeScreen(location: GeoPoint, antipode: GeoPoint) {
    Column(Modifier.fillMaxSize().padding(20.dp)) {
        Text("ANTIPODES", color = Signal, fontSize = 11.sp, fontWeight = FontWeight.Bold, letterSpacing = 2.4.sp)
        Spacer(Modifier.height(5.dp))
        Text("Другая сторона Земли", style = MaterialTheme.typography.headlineMedium)
        Text("Точка, расположенная точно напротив вас", color = Muted, fontSize = 13.sp)
        Spacer(Modifier.height(26.dp))
        Box(
            Modifier.fillMaxWidth().aspectRatio(1f).clip(CircleShape)
                .background(Brush.radialGradient(listOf(Color(0xFF183D33), Color(0xFF08120F))))
                .border(1.dp, Color(0xFF467768), CircleShape), contentAlignment = Alignment.Center
        ) {
            Canvas(Modifier.fillMaxSize().padding(20.dp)) {
                drawCircle(Color(0x2278DDB9), size.minDimension * .36f, center, style = Stroke(1.dp.toPx()))
                drawCircle(Color(0x2278DDB9), size.minDimension * .22f, center, style = Stroke(1.dp.toPx()))
                drawOval(Color(0x2278DDB9), topLeft = Offset(size.width * .08f, size.height * .36f), size = Size(size.width * .84f, size.height * .28f), style = Stroke(1.dp.toPx()))
                drawLine(Mint.copy(.4f), Offset(center.x, size.height * .08f), Offset(center.x, size.height * .92f), 1.dp.toPx())
                drawLine(Mint.copy(.4f), Offset(size.width * .08f, center.y), Offset(size.width * .92f, center.y), 1.dp.toPx())
                drawCircle(Signal.copy(.18f), 22.dp.toPx(), center)
                drawCircle(Signal, 5.dp.toPx(), center)
            }
            Column(horizontalAlignment = Alignment.CenterHorizontally) {
                Spacer(Modifier.height(84.dp))
                Text("ТОЧНЫЙ АНТИПОД", color = Signal, fontSize = 9.sp, fontWeight = FontWeight.Bold, letterSpacing = 1.5.sp)
                Text("${antipode.latitude.format4()}°, ${antipode.longitude.format4()}°", fontWeight = FontWeight.SemiBold, fontSize = 15.sp)
            }
        }
        Spacer(Modifier.height(18.dp))
        Row(horizontalArrangement = Arrangement.spacedBy(10.dp)) {
            CoordinateCard("ВЫ", location, Mint, Modifier.weight(1f))
            CoordinateCard("НАСКВОЗЬ", antipode, Signal, Modifier.weight(1f))
        }
        Spacer(Modifier.height(12.dp))
        Row(
            Modifier.fillMaxWidth().clip(RoundedCornerShape(16.dp)).background(Panel)
                .border(1.dp, Line, RoundedCornerShape(16.dp)).padding(14.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text("≈", color = Signal, fontSize = 24.sp)
            Spacer(Modifier.width(12.dp))
            Column {
                Text("12 742 км сквозь Землю", fontWeight = FontWeight.SemiBold)
                Text("Ближайшая суша будет определена офлайн", color = Muted, fontSize = 11.sp)
            }
        }
    }
}

@Composable
private fun CoordinateCard(label: String, point: GeoPoint, accent: Color, modifier: Modifier = Modifier) {
    Column(modifier.clip(RoundedCornerShape(16.dp)).background(Panel).border(1.dp, Line, RoundedCornerShape(16.dp)).padding(14.dp)) {
        Text(label, color = accent, fontSize = 9.sp, fontWeight = FontWeight.Bold, letterSpacing = 1.5.sp)
        Spacer(Modifier.height(7.dp))
        Text("${point.latitude.format4()}°", fontWeight = FontWeight.Medium, fontSize = 14.sp)
        Text("${point.longitude.format4()}°", color = Muted, fontSize = 13.sp)
    }
}

@Composable
private fun PlaceholderScreen(title: String, symbol: String) {
    Column(
        Modifier.fillMaxSize().padding(28.dp), horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        Box(
            Modifier.size(88.dp).clip(CircleShape).background(Color(0xFF132620)).border(1.dp, Line, CircleShape),
            contentAlignment = Alignment.Center
        ) { Text(symbol, color = Mint, fontSize = 36.sp, fontWeight = FontWeight.Light) }
        Spacer(Modifier.height(20.dp))
        Text(title, style = MaterialTheme.typography.headlineSmall, textAlign = TextAlign.Center)
        Spacer(Modifier.height(8.dp))
        Text("Раздел готовится к следующей экспедиции", color = Muted, fontSize = 13.sp, textAlign = TextAlign.Center)
        Spacer(Modifier.height(18.dp))
        Text(
            "OFFLINE FIRST",
            Modifier.clip(RoundedCornerShape(50)).border(1.dp, Line, RoundedCornerShape(50)).padding(horizontal = 12.dp, vertical = 7.dp),
            color = Mint, fontSize = 9.sp, fontWeight = FontWeight.Bold, letterSpacing = 1.5.sp
        )
    }
}

@Composable
private fun EarthNavigation(selected: Int, onSelect: (Int) -> Unit) {
    val items = listOf("Обзор" to "⌖", "Антипод" to "◉", "Карта" to "⌁", "Места" to "◇", "Профиль" to "○")
    NavigationBar(
        containerColor = Color(0xF508100E), tonalElevation = 0.dp,
        modifier = Modifier.drawBehind { drawLine(Line, Offset.Zero, Offset(size.width, 0f), 1.dp.toPx()) }
    ) {
        items.forEachIndexed { index, item ->
            NavigationBarItem(
                selected = selected == index, onClick = { onSelect(index) },
                icon = { Text(item.second, fontSize = 19.sp, color = if (selected == index) Mint else Muted) },
                label = { Text(item.first, fontSize = 9.sp, fontWeight = if (selected == index) FontWeight.Bold else FontWeight.Normal) },
                colors = NavigationBarItemDefaults.colors(
                    selectedIconColor = Mint, selectedTextColor = Mint, unselectedIconColor = Muted,
                    unselectedTextColor = Muted, indicatorColor = Color(0xFF17362D)
                )
            )
        }
    }
}

@Composable
private fun Metric(label: String, value: String, color: Color = Color.White) {
    Row(verticalAlignment = Alignment.CenterVertically) {
        Text(label, color = Muted, fontSize = 8.sp, fontWeight = FontWeight.Bold, letterSpacing = 1.sp)
        Spacer(Modifier.width(4.dp))
        Text(value, color = color, fontSize = 10.sp, fontWeight = FontWeight.SemiBold)
    }
}

private fun filterLabel(filter: DistanceFilter) = when (filter) {
    DistanceFilter.Nearby -> "Рядом"
    DistanceFilter.Country -> "Страна"
    DistanceFilter.Europe -> "Европа"
    DistanceFilter.World -> "Мир"
    DistanceFilter.Antipodal -> "Антипод"
}

private fun shortestAngle(angle: Float): Float = ((angle + 540f) % 360f) - 180f
private fun normalizeDegrees(angle: Float): Float = ((angle % 360f) + 360f) % 360f
private fun signed(value: Float): String = if (value >= 0) "+${value.roundToInt()}°" else "${value.roundToInt()}°"
private fun formatDistance(km: Double): String = when {
    km < 1.0 -> "${(km * 1000).roundToInt()} M"
    km < 100.0 -> "${km.roundToInt()} KM"
    else -> "${km.roundToInt().toString().reversed().chunked(3).joinToString(" ").reversed()} KM"
}
private fun Double.format1() = String.format(java.util.Locale.US, "%.1f", this)
private fun Double.format4() = String.format(java.util.Locale.US, "%.4f", this)
