package com.transparentearth.data.local

import androidx.room.Database
import androidx.room.RoomDatabase
import androidx.room.Dao
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query
import kotlinx.coroutines.flow.Flow

@Dao interface CityDao { @Query("SELECT * FROM cities ORDER BY importance DESC LIMIT :limit") suspend fun top(limit: Int): List<CityEntity>; @Insert(onConflict = OnConflictStrategy.REPLACE) suspend fun insertAll(cities: List<CityEntity>) }
@Dao interface SavedPlaceDao { @Query("SELECT * FROM saved_places ORDER BY createdAt DESC") fun observeAll(): Flow<List<SavedPlaceEntity>>; @Insert(onConflict = OnConflictStrategy.REPLACE) suspend fun save(place: SavedPlaceEntity) }
@Dao interface LocationHistoryDao { @Insert suspend fun insert(point: LocationHistoryEntity); @Query("DELETE FROM location_history") suspend fun deleteAll() }
@Database(entities = [CityEntity::class, SavedPlaceEntity::class, LocationHistoryEntity::class], version = 1, exportSchema = false)
abstract class AppDatabase : RoomDatabase() { abstract fun cities(): CityDao; abstract fun places(): SavedPlaceDao; abstract fun history(): LocationHistoryDao }
