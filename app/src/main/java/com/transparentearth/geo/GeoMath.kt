package com.transparentearth.geo

import kotlin.math.*

data class GeoPoint(val latitude: Double, val longitude: Double)
data class ProjectedTarget(val distanceKm: Double, val bearingDeg: Double, val verticalAngleDeg: Double)

object GeoMath {
    private const val EARTH_RADIUS_KM = 6371.0088
    const val HALF_CIRCUMFERENCE_KM = Math.PI * EARTH_RADIUS_KM

    fun antipode(point: GeoPoint) = GeoPoint(-point.latitude, normalizeLongitude(point.longitude + 180.0))
    fun normalizeLongitude(value: Double) = ((value + 540.0) % 360.0) - 180.0
    fun distanceKm(from: GeoPoint, to: GeoPoint): Double {
        val dLat = Math.toRadians(to.latitude - from.latitude); val dLon = Math.toRadians(to.longitude - from.longitude)
        val a = sin(dLat / 2).pow(2) + cos(Math.toRadians(from.latitude)) * cos(Math.toRadians(to.latitude)) * sin(dLon / 2).pow(2)
        return 2 * EARTH_RADIUS_KM * asin(sqrt(a))
    }
    fun bearingDeg(from: GeoPoint, to: GeoPoint): Double {
        val dLon = Math.toRadians(to.longitude - from.longitude)
        val y = sin(dLon) * cos(Math.toRadians(to.latitude))
        val x = cos(Math.toRadians(from.latitude)) * sin(Math.toRadians(to.latitude)) - sin(Math.toRadians(from.latitude)) * cos(Math.toRadians(to.latitude)) * cos(dLon)
        return (Math.toDegrees(atan2(y, x)) + 360.0) % 360.0
    }
    /** Geocentric line-of-sight angle: negative values are beneath the local horizon. */
    fun project(from: GeoPoint, to: GeoPoint): ProjectedTarget {
        val distance = distanceKm(from, to); val centralAngle = distance / EARTH_RADIUS_KM
        return ProjectedTarget(distance, bearingDeg(from, to), -Math.toDegrees(centralAngle / 2.0))
    }
}
