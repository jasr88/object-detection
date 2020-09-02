# Unity Object Detection
Object detection app with Unity and Barracuda, just need a ONNX Model

## You will need
  - Unity 2019.4 (LTS) **[Link](https://unity3d.com/unity/qa/lts-releases?_ga=2.58722797.174822135.1598914732-159074198.1598457295)**
  - Barracuda 1.0.2 (already instlled via package manager)
  - An ONNX mode, you will find one in the Assets/Model/model.onnx *(the model is trained to detect sneakers... but also detects chanclas)*
    - You can train your own models with **[Custom Vision](https://azure.microsoft.com/es-mx/services/cognitive-services/custom-vision-service/)**

## Based on
This code is based on the Microsoft's tutorial **[Tutorial: Detect objects using ONNX in ML.NET](https://docs.microsoft.com/en-us/dotnet/machine-learning/tutorials/object-detection-onnx?ranMID=24542&ranEAID=je6NUbpObpQ&ranSiteID=je6NUbpObpQ-KmOmjkg1OEDpIAWtcQ0wrA&epi=je6NUbpObpQ-KmOmjkg1OEDpIAWtcQ0wrA&irgwc=1&OCID=AID2000142_aff_7593_1243925&tduid=%28ir__b6vvowqktokfthyjkk0sohzgc32xireljywncfnh00%29%287593%29%281243925%29%28je6NUbpObpQ-KmOmjkg1OEDpIAWtcQ0wrA%29%28%29&irclickid=_b6vvowqktokfthyjkk0sohzgc32xireljywncfnh00)** with some modifications that make it works in Unity with barracuda instead ML.NET 
