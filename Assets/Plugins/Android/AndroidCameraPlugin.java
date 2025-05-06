package com.yourcompany.androidcamera;

import android.Manifest;
import android.content.Context;
import android.content.pm.PackageManager;
import android.hardware.Camera;
import android.view.SurfaceHolder;
import android.view.SurfaceView;
import android.view.ViewGroup;
import android.widget.FrameLayout;
import android.os.Handler;
import android.os.Looper;
import android.util.Log;
import android.graphics.ImageFormat;
import android.graphics.YuvImage;
import android.graphics.Rect;
import java.io.ByteArrayOutputStream;
import java.nio.ByteBuffer;
import android.util.Base64;
import java.util.List;

import com.unity3d.player.UnityPlayer;

public class AndroidCameraPlugin {
    private static final String TAG = "AndroidCameraPlugin";
    private Camera camera;
    private SurfaceView surfaceView;
    private boolean isPreviewRunning = false;
    private Context context;
    private Handler mainHandler;
    private int previewWidth = 640;  // Square resolution
    private int previewHeight = 640; // Square resolution
    private byte[] imageBuffer;
    private Camera.PreviewCallback previewCallback;

    public AndroidCameraPlugin() {
        context = UnityPlayer.currentActivity;
        mainHandler = new Handler(Looper.getMainLooper());
    }

    private Camera.Size getOptimalPreviewSize(List<Camera.Size> sizes, int w, int h) {
        final double ASPECT_TOLERANCE = 0.1;
        double targetRatio = (double) w / h;

        if (sizes == null) return null;

        Camera.Size optimalSize = null;
        double minDiff = Double.MAX_VALUE;

        // First try to find a size that matches the target ratio
        for (Camera.Size size : sizes) {
            double ratio = (double) size.width / size.height;
            if (Math.abs(ratio - targetRatio) > ASPECT_TOLERANCE) continue;
            if (Math.abs(size.height - h) < minDiff) {
                optimalSize = size;
                minDiff = Math.abs(size.height - h);
            }
        }

        // If we can't find a size that matches the ratio, find the closest size
        if (optimalSize == null) {
            minDiff = Double.MAX_VALUE;
            for (Camera.Size size : sizes) {
                if (Math.abs(size.height - h) < minDiff) {
                    optimalSize = size;
                    minDiff = Math.abs(size.height - h);
                }
            }
        }

        // If we still can't find a suitable size, try to find the closest square resolution
        if (optimalSize == null) {
            for (Camera.Size size : sizes) {
                if (size.width <= 640 && size.height <= 640) {
                    if (optimalSize == null || (size.width * size.height) > (optimalSize.width * optimalSize.height)) {
                        optimalSize = size;
                    }
                }
            }
        }

        return optimalSize;
    }

    public void startCamera() {
        mainHandler.post(new Runnable() {
            @Override
            public void run() {
                if (context.checkSelfPermission(Manifest.permission.CAMERA) != PackageManager.PERMISSION_GRANTED) {
                    Log.e(TAG, "Camera permission not granted");
                    return;
                }

                try {
                    camera = Camera.open();
                    if (camera == null) {
                        Log.e(TAG, "Failed to open camera");
                        return;
                    }

                    Camera.Parameters parameters = camera.getParameters();
                    
                    // Get supported preview sizes
                    List<Camera.Size> supportedSizes = parameters.getSupportedPreviewSizes();
                    Camera.Size optimalSize = getOptimalPreviewSize(supportedSizes, previewWidth, previewHeight);
                    
                    if (optimalSize != null) {
                        previewWidth = optimalSize.width;
                        previewHeight = optimalSize.height;
                        Log.d(TAG, "Using camera resolution: " + previewWidth + "x" + previewHeight);
                    }

                    // Set camera parameters
                    parameters.setPreviewSize(previewWidth, previewHeight);
                    parameters.setPreviewFormat(ImageFormat.NV21);
                    
                    // Try to set focus mode to continuous
                    List<String> focusModes = parameters.getSupportedFocusModes();
                    if (focusModes.contains(Camera.Parameters.FOCUS_MODE_CONTINUOUS_PICTURE)) {
                        parameters.setFocusMode(Camera.Parameters.FOCUS_MODE_CONTINUOUS_PICTURE);
                    }
                    
                    // Try to set scene mode to barcode
                    List<String> sceneModes = parameters.getSupportedSceneModes();
                    if (sceneModes.contains(Camera.Parameters.SCENE_MODE_BARCODE)) {
                        parameters.setSceneMode(Camera.Parameters.SCENE_MODE_BARCODE);
                    }

                    camera.setParameters(parameters);

                    // Calculate buffer size
                    int bufferSize = previewWidth * previewHeight * ImageFormat.getBitsPerPixel(ImageFormat.NV21) / 8;
                    imageBuffer = new byte[bufferSize];

                    // Set up preview callback
                    previewCallback = new Camera.PreviewCallback() {
                        @Override
                        public void onPreviewFrame(byte[] data, Camera camera) {
                            if (data != null) {
                                // Convert YUV to JPEG
                                YuvImage yuv = new YuvImage(data, ImageFormat.NV21, previewWidth, previewHeight, null);
                                ByteArrayOutputStream out = new ByteArrayOutputStream();
                                yuv.compressToJpeg(new Rect(0, 0, previewWidth, previewHeight), 80, out); // Reduced JPEG quality
                                byte[] jpegData = out.toByteArray();
                                
                                // Convert to Base64 string for Unity
                                String base64Data = Base64.encodeToString(jpegData, Base64.NO_WRAP);
                                UnityPlayer.UnitySendMessage("WebCamController", "OnCameraFrame", base64Data);
                            }
                        }
                    };

                    // Create SurfaceView for camera preview
                    surfaceView = new SurfaceView(context);
                    surfaceView.getHolder().addCallback(new SurfaceHolder.Callback() {
                        @Override
                        public void surfaceCreated(SurfaceHolder holder) {
                            try {
                                camera.setPreviewDisplay(holder);
                                camera.setPreviewCallback(previewCallback);
                                camera.startPreview();
                                isPreviewRunning = true;
                                Log.d(TAG, "Camera preview started");
                            } catch (Exception e) {
                                Log.e(TAG, "Error starting camera preview: " + e.getMessage());
                            }
                        }

                        @Override
                        public void surfaceChanged(SurfaceHolder holder, int format, int width, int height) {
                            if (camera != null) {
                                try {
                                    camera.stopPreview();
                                    Camera.Parameters parameters = camera.getParameters();
                                    parameters.setPreviewSize(width, height);
                                    camera.setParameters(parameters);
                                    camera.startPreview();
                                } catch (Exception e) {
                                    Log.e(TAG, "Error changing camera preview: " + e.getMessage());
                                }
                            }
                        }

                        @Override
                        public void surfaceDestroyed(SurfaceHolder holder) {
                            if (camera != null) {
                                camera.stopPreview();
                                isPreviewRunning = false;
                            }
                        }
                    });

                    // Add SurfaceView to Unity's view hierarchy
                    ViewGroup rootView = (ViewGroup) UnityPlayer.currentActivity.findViewById(android.R.id.content);
                    FrameLayout.LayoutParams layoutParams = new FrameLayout.LayoutParams(
                            ViewGroup.LayoutParams.MATCH_PARENT,
                            ViewGroup.LayoutParams.MATCH_PARENT);
                    rootView.addView(surfaceView, layoutParams);

                } catch (Exception e) {
                    Log.e(TAG, "Error starting camera: " + e.getMessage());
                }
            }
        });
    }

    public void stopCamera() {
        mainHandler.post(new Runnable() {
            @Override
            public void run() {
                if (camera != null) {
                    camera.stopPreview();
                    camera.setPreviewCallback(null);
                    camera.release();
                    camera = null;
                    isPreviewRunning = false;
                }
                if (surfaceView != null) {
                    ViewGroup rootView = (ViewGroup) UnityPlayer.currentActivity.findViewById(android.R.id.content);
                    rootView.removeView(surfaceView);
                    surfaceView = null;
                }
            }
        });
    }

    public boolean isCameraRunning() {
        return isPreviewRunning;
    }
} 