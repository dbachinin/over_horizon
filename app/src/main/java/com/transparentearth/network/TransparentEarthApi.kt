package com.transparentearth.network

import retrofit2.http.*

/** Backend boundary: UI and Room never depend on this interface. */
interface TransparentEarthApi {
 @POST("auth/google") suspend fun signInWithGoogle(@Body token: Map<String, String>): Map<String, String>
 @GET("me") suspend fun me(): Map<String, String>
 @PATCH("me") suspend fun updateMe(@Body fields: Map<String, String?>)
 @DELETE("me") suspend fun deleteAccount()
 @GET("places") suspend fun places(): List<Map<String, String>>
 @POST("locations/batch") suspend fun uploadLocations(@Body locations: List<Map<String, String>>)
 @DELETE("locations") suspend fun deleteLocations()
}
