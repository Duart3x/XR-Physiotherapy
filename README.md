# Unity Sample

This sample demonstrate how to work with Sensor and Body data streams from Unity
and how to animate 3D character using body data.

## Preparations

Before first opening a Unity project, run `prepare.cmd`.
It will copy necessary binaries to Unity project folders.

The script assumes that **Azure Kinect Body Tracking SDK** is installed into default location under Program Files.
If it doesn't take place then copy the following files from `tools` folder of Body Tracking SDK to `Assets\Plugins\K4AdotNet` folder of this plugin:
* `k4abt.dll`,
* `dnn_model_2_0_op11.onnx`,
* `dnn_model_2_0_lite_op11.onnx`,
* `cublas64_11.dll`,
* `cublasLt64_11.dll`,
* `cudart64_110.dll`,
* `cudnn_cnn_infer64_8.dll`,
* `cudnn_ops_infer64_8.dll`,
* `cudnn64_8.dll`,
* `cufft64_10.dll`,
* `onnxruntime.dll`,
* `vcomp140.dll`.

As a rule `prepare.cmd` does the trick, but you can copy all dependencies to `Assets\Plugins\K4AdotNet` folder of this plugin manually. 