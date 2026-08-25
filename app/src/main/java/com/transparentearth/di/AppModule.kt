package com.transparentearth.di

import android.content.Context
import androidx.room.Room
import com.transparentearth.data.local.*
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.android.qualifiers.ApplicationContext
import dagger.hilt.components.SingletonComponent
import javax.inject.Singleton

@Module @InstallIn(SingletonComponent::class) object AppModule {
 @Provides @Singleton fun database(@ApplicationContext context: Context) = Room.databaseBuilder(context, AppDatabase::class.java, "transparent-earth.db").fallbackToDestructiveMigration().build()
 @Provides fun cities(db: AppDatabase): CityDao = db.cities()
 @Provides fun places(db: AppDatabase): SavedPlaceDao = db.places()
 @Provides fun history(db: AppDatabase): LocationHistoryDao = db.history()
}
