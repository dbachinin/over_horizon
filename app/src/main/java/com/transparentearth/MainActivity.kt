package com.transparentearth

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.enableEdgeToEdge
import androidx.activity.compose.setContent
import dagger.hilt.android.AndroidEntryPoint
import com.transparentearth.ui.TransparentEarthRoot

@AndroidEntryPoint
class MainActivity : ComponentActivity() {
 override fun onCreate(savedInstanceState: Bundle?) {
  super.onCreate(savedInstanceState)
  enableEdgeToEdge()
  setContent { TransparentEarthRoot() }
 }
}
