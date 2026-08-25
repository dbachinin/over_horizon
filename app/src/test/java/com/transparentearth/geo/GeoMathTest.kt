package com.transparentearth.geo

import org.junit.Assert.*
import org.junit.Test

class GeoMathTest {
 @Test fun antipode_handles_antimeridian() { assertEquals(GeoPoint(-10.0, -160.0), GeoMath.antipode(GeoPoint(10.0, 20.0))) }
 @Test fun same_point_has_zero_distance() { assertEquals(0.0, GeoMath.distanceKm(GeoPoint(0.0, 0.0), GeoPoint(0.0, 0.0)), 0.0001) }
 @Test fun antipode_is_half_circumference() { assertEquals(GeoMath.HALF_CIRCUMFERENCE_KM, GeoMath.distanceKm(GeoPoint(0.0, 0.0), GeoPoint(0.0, 180.0)), 0.1) }
}
