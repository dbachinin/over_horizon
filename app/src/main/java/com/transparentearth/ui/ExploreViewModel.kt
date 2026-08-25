package com.transparentearth.ui

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.transparentearth.data.CityRepository
import com.transparentearth.data.local.CityEntity
import com.transparentearth.geo.GeoMath
import com.transparentearth.geo.GeoPoint
import com.transparentearth.sensors.OrientationProvider
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch
import javax.inject.Inject

enum class DistanceFilter(val limitKm: Double) { Nearby(300.0), Country(900.0), Europe(3500.0), World(10000.0), Antipodal(GeoMath.HALF_CIRCUMFERENCE_KM) }
data class ExploreState(val location: GeoPoint = GeoPoint(44.7866, 20.4489), val transparentEarth: Boolean = false, val filter: DistanceFilter = DistanceFilter.Nearby, val heading: Float = 0f, val pitch: Float = 0f, val cities: List<CityEntity> = emptyList())

@HiltViewModel class ExploreViewModel @Inject constructor(private val cities: CityRepository, orientationProvider: OrientationProvider) : ViewModel() {
 private val mutable = MutableStateFlow(ExploreState())
 val state: StateFlow<ExploreState> = mutable.asStateFlow()
 init { viewModelScope.launch { cities.seedIfEmpty(); reload() }; viewModelScope.launch { orientationProvider.orientation.collect { mutable.update { s -> s.copy(heading = it.headingDeg, pitch = it.pitchDeg) } } } }
 fun setTransparent(enabled: Boolean) { mutable.update { it.copy(transparentEarth = enabled, filter = if (enabled) DistanceFilter.Antipodal else it.filter) }; reload() }
 fun setFilter(filter: DistanceFilter) { mutable.update { it.copy(filter = filter) }; reload() }
 private fun reload() = viewModelScope.launch { val s = state.value; mutable.update { it.copy(cities = cities.candidates(s.location, if (s.transparentEarth) GeoMath.HALF_CIRCUMFERENCE_KM else s.filter.limitKm)) } }
 fun antipode() = GeoMath.antipode(state.value.location)
}
