package com.transparentearth.sensors

import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import javax.inject.Inject
import javax.inject.Singleton

data class Orientation(val headingDeg: Float = 0f, val pitchDeg: Float = 0f)
/** Sensor integration point. It intentionally exposes state, so Compose only redraws and never performs sensor work. */
@Singleton class OrientationProvider @Inject constructor() {
 private val mutable = MutableStateFlow(Orientation())
 val orientation: StateFlow<Orientation> = mutable
 fun update(heading: Float, pitch: Float) { mutable.value = Orientation(heading, pitch) }
}
