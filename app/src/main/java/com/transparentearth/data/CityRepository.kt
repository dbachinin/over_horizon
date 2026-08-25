package com.transparentearth.data

import com.transparentearth.data.local.CityDao
import com.transparentearth.data.local.CityEntity
import com.transparentearth.geo.GeoMath
import com.transparentearth.geo.GeoPoint
import javax.inject.Inject
import javax.inject.Singleton

@Singleton class CityRepository @Inject constructor(private val cityDao: CityDao) {
    suspend fun candidates(origin: GeoPoint, maxDistanceKm: Double): List<CityEntity> = cityDao.top(500).filter { GeoMath.distanceKm(origin, GeoPoint(it.latitude, it.longitude)) <= maxDistanceKm }
    suspend fun seedIfEmpty() { if (cityDao.top(1).isEmpty()) cityDao.insertAll(seedCities) }
    private val seedCities = listOf(
        CityEntity("belgrade", "Belgrade", "Belgrade", "RS", 44.7866, 20.4489, 1400000, 0.90),
        CityEntity("budapest", "Budapest", "Budapest", "HU", 47.4979, 19.0402, 1750000, 0.92),
        CityEntity("tokyo", "Tokyo", "Tokyo", "JP", 35.6762, 139.6503, 13960000, 1.0),
        CityEntity("new-york", "New York", "New York", "US", 40.7128, -74.0060, 8800000, 1.0),
        CityEntity("sydney", "Sydney", "Sydney", "AU", -33.8688, 151.2093, 5300000, 0.97),
        CityEntity("buenos-aires", "Buenos Aires", "Buenos Aires", "AR", -34.6037, -58.3816, 3000000, 0.94))
}
