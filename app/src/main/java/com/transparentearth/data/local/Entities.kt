package com.transparentearth.data.local

import androidx.room.Entity
import androidx.room.PrimaryKey

@Entity(tableName = "cities")
data class CityEntity(@PrimaryKey val id: String, val name: String, val asciiName: String, val countryCode: String, val latitude: Double, val longitude: Double, val population: Long, val importance: Double)
@Entity(tableName = "saved_places")
data class SavedPlaceEntity(@PrimaryKey val id: String, val name: String, val latitude: Double, val longitude: Double, val notes: String = "", val createdAt: Long = System.currentTimeMillis())
@Entity(tableName = "location_history")
data class LocationHistoryEntity(@PrimaryKey(autoGenerate = true) val id: Long = 0, val latitude: Double, val longitude: Double, val altitude: Double?, val accuracy: Float?, val timestamp: Long, val source: String)
