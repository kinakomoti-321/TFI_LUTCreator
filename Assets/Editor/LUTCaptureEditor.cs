using UnityEditor;
using UnityEngine;
using System.Collections;
public class LUTCaptureEditor : EditorWindow
{
    private Camera captureCamera;
    private GameObject cameraObject;
    private GameObject planeObject;
    private Material captureMaterial;
    private RenderTexture renderTexture;
    private Texture2D lutTexture;
    private GameObject previewObject;
    private Material previewMaterial;

    //Window Scroll
    private Vector2 scrollPos;

    //Texture Parameters
    private int lutSize = 1024;
    private int _textureSizeOp = 5;
    private string[] _textureSizeOptions = new string[] {"32","64","128","256","512","1024","2048","4096"};


    //Material Parameters
    private float _topLayerIOR = 1.0f;
    private float _middleLayerIOR = 1.33f;
    private float _middleLayerMinimamThickness = 000.0f;
    private float _middleLayerMaximamThickness= 1000.0f;
    private Vector3 _bottomLayerIOR = new Vector3(1.0f,1.0f,1.0f);
    private Vector3 _bottomLayerKappa =new Vector3(0.0f,0.0f,0.0f);

    //PreviewSetting
    private float _previewLayerThickness = 500;
    private RenderTexture _previewRenderTexture;
    private Texture2D _previewTexture;
    private Camera _previewCamera;
    private GameObject _previewCameraObject;

    //Foldout
    private bool _foldOutTexture = false;
    private bool _foldOutInterference = false;
    private bool _foldOutIOR = false;
    private bool _foldPreview = false;

    //IOR Example
    private string[] objectNames ;
    private Vector3[] objectIORs;
    private Vector3[] objectCoefficients;

    //Color
    private Color _bottomLayerBaseColor = Color.white;
    private Color _bottomLayerEdgeTint = Color.white;

    //Popup
    private int _selectedOption = 0;
    private string[] _options = new string[] {"Dielectric IOR","Color Control Metallic CIRO","Direct control CIOR"};

    [MenuItem("Window/LUT Capture")]
    private static void ShowWindow()
    {
        LUTCaptureEditor window = GetWindow<LUTCaptureEditor>();
        window.titleContent = new GUIContent("LUT Capture");
        window.Show();

    }

    private void OnEnable()
    {
        objectIORs = new Vector3[]{ 
            new Vector3(1.0f,1.0f,1.0f),
            new Vector3(1.46f,1.46f,1.46f),
            new Vector3(1.33f,1.33f,1.33f),
            new Vector3(1.33f,1.33f,1.33f),
            new Vector3(1.46f,1.46f,1.46f),
            new Vector3(3.01f,3.01f,3.01f),
            new Vector3(2.63f,2.63f,2.63f),
            new Vector3(1.63f,1.63f,1.63f),
            new Vector3(2.87f,2.94f,2.51f),
            new Vector3(1.92f,1.00f,0.58f),
            new Vector3(0.21f,1.03f,1.24f),
            new Vector3(0.04f,0.05f,0.04f),
            new Vector3(0.13f,0.44f,1.43f)
        };

        objectCoefficients =  new Vector3[]{
            new Vector3(0.0f,0.0f,0.0f),
            new Vector3(0.0f,0.0f,0.0f),
            new Vector3(0.0f,0.0f,0.0f),
            new Vector3(0.0f,0.0f,0.0f),
            new Vector3(0.0f,0.0f,0.0f),
            new Vector3(0.0f,0.0f,0.0f),
            new Vector3(0.0f,0.0f,0.0f),
            new Vector3(0.0f,0.0f,0.0f),
            new Vector3(3.18f,2.92f,2.72f),
            new Vector3(8.14f,6.58f,5.28f),
            new Vector3(4.15f,2.57f,2.32f),
            new Vector3(4.80f,3.56f,2.50f),
            new Vector3(4.06f,2.41f,1.94f)
        };
        
        objectNames = new string[]{
            "大気(空気)",
            "ガラス(SiO2)", 
            "水(H2O)",
            "石鹸水",
            "油(オリーブオイル)",
            "酸化鉄 Fe2O2",
            "酸化銅(II)", 
            "酸化アルミニウム",
            "鉄 Fe" ,
            "アルミニウム",
            "銅 Cu",
            "銀 Ag",
            "金 Au"
            
        };
        setTextureResolution(256);
        setPreviewTexture();
    }

    private void OnDisable()
    {
        if(captureCamera.name == "LUTCaptureCamera")
        {
            DestroyImmediate(captureCamera.gameObject);
        }
        if(previewObject.name == "LUTPreviewObject")
        {
            DestroyImmediate(previewObject);
        }
        if(_previewCamera.name == "LUTPreviewCamera")
        {
            DestroyImmediate(_previewCamera.gameObject);
        }
        if(planeObject.name == "LUTPlane"){
            DestroyImmediate(planeObject);
        }

        DestroyImmediate(captureMaterial);
        DestroyImmediate(previewMaterial);
        DestroyImmediate(lutTexture);
        DestroyImmediate(renderTexture);
        DestroyImmediate(_previewRenderTexture);
        DestroyImmediate(_previewTexture);
    }
    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        captureCamera = EditorGUILayout.ObjectField("Capture Camera", captureCamera, typeof(Camera), true) as Camera;
        planeObject = EditorGUILayout.ObjectField("LUT Plane", planeObject, typeof(GameObject), true) as GameObject;
        captureMaterial = EditorGUILayout.ObjectField("Capture Material", captureMaterial, typeof(Material), true) as Material;


        GUILayout.Space(10);
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Initialize", GUILayout.Height(100),GUILayout.Width(100)))
            {
                CreateLUTCaptureObjects();
            }
            if (GUILayout.Button("Capture LUT",GUILayout.Height(100),GUILayout.Width(100)))
            {
                if (captureCamera != null && planeObject != null && captureMaterial != null)
                {
                    CaptureLUT();
                }
                else
                {
                    Debug.LogWarning("Please assign the Capture Camera, Plane Object, and Capture Material!");
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        
        GUILayout.Space(20);

        Rect rect = GUILayoutUtility.GetLastRect();
        EditorGUI.DrawPreviewTexture(new Rect(50,rect.y + rect.height + 10, 256, 256), lutTexture);        
        EditorGUI.DrawPreviewTexture(new Rect(70 + 256,rect.y + rect.height + 10, 256, 256), _previewTexture);        

        GUILayout.Space(256 + 30);

        _foldPreview = EditorGUILayout.Foldout(_foldPreview,"Preview Setting");
        if(_foldPreview){
            previewObject = EditorGUILayout.ObjectField("LUT Preview Object", previewObject, typeof(GameObject), true) as GameObject;
            previewMaterial = EditorGUILayout.ObjectField("LUT Preview Material", previewMaterial, typeof(Material), true) as Material;
            _previewCamera = EditorGUILayout.ObjectField("Preview Camera", _previewCamera, typeof(Camera), true) as Camera;
            _previewLayerThickness = EditorGUILayout.FloatField("Layer Thickness",_previewLayerThickness);
        }

        GUILayout.Space(20);

        //Texture Setting
        _foldOutTexture = EditorGUILayout.Foldout(_foldOutTexture,"Texture Setting");
        if(_foldOutTexture){
            _textureSizeOp = EditorGUILayout.Popup("Texture Size", _textureSizeOp, _textureSizeOptions); 
            switch(_textureSizeOp){
                case 0:
                    lutSize = 32;
                    break;
                case 1:
                    lutSize = 64;
                    break;
                case 2:
                    lutSize = 128;
                    break;
                case 3:
                    lutSize = 256;
                    break;
                case 4:
                    lutSize = 512;
                    break;
                case 5:
                    lutSize = 1024;
                    break;
                case 6:
                    lutSize = 2048;
                    break;
                case 7:
                    lutSize = 4096;
                    break;
            }

            _middleLayerMinimamThickness = EditorGUILayout.FloatField("Minimam Thickness (nm)",_middleLayerMinimamThickness);
            _middleLayerMaximamThickness = EditorGUILayout.FloatField("Maximam Thickness (nm)",_middleLayerMaximamThickness);
        }

        GUILayout.Space(20);

        //Interference Setting 

        _foldOutInterference = EditorGUILayout.Foldout(_foldOutInterference,"Thin-Film Interference Setting");
        if(_foldOutInterference){
            GUILayout.Space(10);
            if(GUILayout.Button("Default Parameter",GUILayout.Height(30),GUILayout.Width(200))){
                SetMaterialParametersDefault();
            }
            GUILayout.Space(10);

            _topLayerIOR = EditorGUILayout.FloatField("Top Layer IOR",_topLayerIOR);
            _middleLayerIOR = EditorGUILayout.FloatField("Middle Layer IOR",_middleLayerIOR);
            _selectedOption = EditorGUILayout.Popup("Bottom Layer CIOR", _selectedOption, _options);
            switch(_selectedOption){
                case 0:
                    GUILayout.Label("Dielectric");
                    _bottomLayerIOR.x = EditorGUILayout.FloatField("Dielectric IOR",_bottomLayerIOR.x);
                    _bottomLayerIOR.y = _bottomLayerIOR.x;
                    _bottomLayerIOR.z = _bottomLayerIOR.x;
                    _bottomLayerKappa = Vector3.zero;
                    ciorToRGB(_bottomLayerIOR,_bottomLayerKappa,ref _bottomLayerBaseColor,ref _bottomLayerEdgeTint);

                    break;
                case 1:
                    GUILayout.Label("Conductor Fresnel Color");
                    _bottomLayerBaseColor = EditorGUILayout.ColorField("Base Color",_bottomLayerBaseColor);
                    _bottomLayerEdgeTint = EditorGUILayout.ColorField("Edge Tint",_bottomLayerEdgeTint);
                    rgbToIOR(ref _bottomLayerIOR,ref _bottomLayerKappa,_bottomLayerBaseColor,_bottomLayerEdgeTint);

                    break;
                case 2:
                    GUILayout.Label("Complex Index of Refraction");
                    _bottomLayerIOR = EditorGUILayout.Vector3Field("Bottom Layer IOR",_bottomLayerIOR);
                    _bottomLayerKappa = EditorGUILayout.Vector3Field("Bottom Layer Extinction Coffiencient",_bottomLayerKappa); 
                    ciorToRGB(_bottomLayerIOR,_bottomLayerKappa,ref _bottomLayerBaseColor,ref _bottomLayerEdgeTint);

                    break;
            }

            _foldOutIOR = EditorGUILayout.Foldout(_foldOutIOR,"IOR Example");


            if(_foldOutIOR){
                EditorGUILayout.LabelField("誘電体のIORはRGB全て同じ値として、基本的にナトリウムd線(589.3nm)の値を記載しています");
                EditorGUILayout.LabelField("金属のIOR及びKappaはCIEに倣いRGBそれぞれを700nm,546.1nm,435.8nmの単色光とみなした時の値を記載しています");
                EditorGUILayout.LabelField("金属はそのまま入れても現実っぽくならないので調節が必要です");

                DrawTableHeader();

                for (int i = 0; i < objectNames.Length; i++)
                {
                    DrawTableRow(objectNames[i], objectIORs[i], objectCoefficients[i]); // テーブルの行を描画
                }
            }
            

        }

        GUILayout.Space(20);


        GUILayout.Space(20);
        EditorGUILayout.EndScrollView();

        if(captureMaterial != null){
            SetMaterialParameters();
        }
        if(previewMaterial != null){
            SetPreviewMaterialParameters();
        }
        
        if(_previewCamera != null){
            PreviewRender();
        }
        if(captureCamera != null){
            RenderPreviewLUT();
        }
    }
    
    private void setTextureResolution(int texSize){
        renderTexture = new RenderTexture(texSize,texSize,0,RenderTextureFormat.ARGB32); 
        renderTexture.filterMode = FilterMode.Bilinear;
        renderTexture.Create();
        lutTexture = new Texture2D(texSize,texSize,TextureFormat.ARGB32,false);
        lutTexture.filterMode = FilterMode.Bilinear;
        lutTexture.wrapMode = TextureWrapMode.Clamp;

        //Debug.Log("Texture Resolution Changed to " + texSize.ToString() + "x" + texSize.ToString());
    }

    private void setPreviewTexture(){
        
        _previewRenderTexture = new RenderTexture(256,256,0,RenderTextureFormat.ARGB32);
        _previewRenderTexture.Create();
        _previewTexture = new Texture2D(256,256,TextureFormat.ARGB32,false);
        _previewTexture.filterMode = FilterMode.Bilinear;
        _previewTexture.wrapMode = TextureWrapMode.Clamp;
    }

    private void PreviewRender(){
        _previewCamera.targetTexture = _previewRenderTexture;
        _previewCamera.Render();
        RenderTexture.active = _previewRenderTexture;

        _previewTexture.ReadPixels(new Rect(0,0,256,256),0,0);
        _previewTexture.Apply();
        RenderTexture.active = null;
    }
    private void DrawTableHeader()
    {
        GUILayout.BeginHorizontal();
        DrawTableCell("名前", 120, true); // ヘッダーセルを色濃く描画
        DrawTableCell("IOR", 120, true); // ヘッダーセルを色濃く描画
        DrawTableCell("Kappa", 120, true); // ヘッダーセルを色濃く描画
        GUILayout.EndHorizontal();
    }

    private void DrawTableRow(string name, Vector3 ior, Vector3 coefficient)
    {
        GUILayout.BeginHorizontal();
        DrawTableCell(name, 120);
        DrawTableCell("(" + ior.x.ToString() + "," + ior.y.ToString() + "," + ior.z.ToString() + ")", 120);
        DrawTableCell("(" + coefficient.x.ToString() + "," + coefficient.y.ToString() + "," + coefficient.z.ToString() + ")", 120);
        GUILayout.EndHorizontal();
    }

    private void DrawTableCell(string text, float width, bool isHeader = false)
    {
        GUIStyle cellStyle = new GUIStyle(GUI.skin.box);
        if (isHeader)
        {
            cellStyle.normal.textColor = Color.white;
            cellStyle.normal.background = Texture2D.grayTexture;
        }

        GUILayout.Box(text, cellStyle, GUILayout.Width(width));
    } 

    private void RenderPreviewLUT(){
        RenderTexture.active = renderTexture;
        captureCamera.targetTexture = renderTexture;

        captureCamera.Render();

        lutTexture.ReadPixels(new Rect(0, 0, lutSize, lutSize), 0, 0);
        lutTexture.Apply();

        RenderTexture.active = null;

    }
    private void RenderLUT(){
        setTextureResolution(lutSize);
        RenderTexture.active = renderTexture;
        captureCamera.targetTexture = renderTexture;

        captureCamera.Render();

        lutTexture.ReadPixels(new Rect(0, 0, lutSize, lutSize), 0, 0);
        lutTexture.Apply();

        RenderTexture.active = null;
    }


    private void CaptureLUT()
    {
        RenderLUT();
        SaveLUT();
        setTextureResolution(256);
    }

    private void SaveLUT()
    {
        string filePath = EditorUtility.SaveFilePanel("Save LUT", "", 
        "TFI_LUT_ior(" + _bottomLayerIOR.x.ToString() + "_" + _bottomLayerIOR.y.ToString() + "_" + _bottomLayerIOR.z.ToString() + ")_Kappa(" + _bottomLayerKappa.x.ToString() + "_" + _bottomLayerKappa.y.ToString() + "_" + _bottomLayerKappa.z.ToString() + ")"
        + "_MiddleLayerIOR_" + _middleLayerIOR.ToString() + "_Thickness(" + _middleLayerMinimamThickness.ToString() + "_" + _middleLayerMaximamThickness.ToString() + ")", "png");
        if (string.IsNullOrEmpty(filePath))
            return;

        byte[] bytes = lutTexture.EncodeToPNG();
        System.IO.File.WriteAllBytes(filePath, bytes);
        AssetDatabase.Refresh();

        Debug.Log("Texture Size: " + lutTexture.width.ToString() + "x" + lutTexture.height.ToString());
        Debug.Log("MiddleLapyerIOR: " + _middleLayerIOR.ToString());
        Debug.Log("MiddleLayerThickness: " + _middleLayerMinimamThickness.ToString() + " ~ " + _middleLayerMaximamThickness.ToString());
        Debug.Log("BottomLayerIOR: " + _bottomLayerIOR.ToString());
        Debug.Log("BottomLayerKappa: " + _bottomLayerKappa.ToString());
        Debug.Log("LUT saved to: " + filePath);
    }

    private void CreateLUTCaptureObjects()
    {
        var findcamera = GameObject.Find("LUTCaptureCamera");
        var findplane = GameObject.Find("LUTPlane");

        if (findcamera == null)
        {
            GameObject captureCameraObj = new GameObject("LUTCaptureCamera");
            cameraObject = captureCameraObj;
            captureCamera = captureCameraObj.AddComponent<Camera>();
        }
        else{
            cameraObject = findcamera;
            captureCamera = findcamera.GetComponent<Camera>();
        }

        if(findplane == null){
            GameObject planeObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
            planeObject = planeObj;
        }
        else{
            planeObject = findplane;
        }

        if(captureMaterial == null){
            captureMaterial = new Material(Shader.Find("TFI_LUTCreator/InterferenceLUT"));
        }

        int LUTlayer = 8;
        planeObject.name = "LUTPlane";
        planeObject.GetComponent<MeshRenderer>().receiveShadows = false;
        planeObject.GetComponent<MeshRenderer>().sharedMaterial = captureMaterial;
        planeObject.layer = LUTlayer;

        planeObject.transform.position = new Vector3(0, 0, 0);
        planeObject.transform.rotation = Quaternion.Euler(0, 0, 0);
        planeObject.transform.localScale = new Vector3(1,1,1);

        cameraObject.transform.position = new Vector3(0, 1, 0);
        cameraObject.transform.rotation = Quaternion.Euler(90,180, 0);
        cameraObject.transform.localScale = new Vector3(1,1,1);

        captureCamera.orthographic = true;
        captureCamera.cullingMask = 1 << LUTlayer;

        if(previewMaterial == null){
            previewMaterial = new Material(Shader.Find("TFI_LUTCreator/LUT_Viewer"));
        }

        var findPreviewObj = GameObject.Find("LUTPreviewObject");
        if(findPreviewObj == null){
            previewObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        }
        else{
            previewObject = findPreviewObj;
        }

        previewObject.name = "LUTPreviewObject";
        previewObject.GetComponent<MeshRenderer>().receiveShadows = false;
        previewObject.GetComponent<MeshRenderer>().sharedMaterial = previewMaterial;
        previewObject.transform.position = new Vector3(8, 0, 0);
        previewObject.transform.localScale = new Vector3(4,4,4);

        var findPreviewCamera = GameObject.Find("LUTPreviewCamera");
        if(findPreviewCamera == null){
            _previewCameraObject = new GameObject("LUTPreviewCamera");
            _previewCamera = _previewCameraObject.AddComponent<Camera>();
        }
        else{
            _previewCameraObject = findPreviewCamera;
            _previewCamera = findPreviewCamera.GetComponent<Camera>();
        }

        _previewCameraObject.transform.position = new Vector3(8, 0, 4);
        _previewCameraObject.transform.rotation = Quaternion.Euler(0,180, 0);
        _previewCameraObject.transform.localScale = new Vector3(1,1,1);
    }

    private void SetMaterialParametersDefault(){
        _topLayerIOR = 1.0f;
        _middleLayerIOR = 1.33f;
        _middleLayerMinimamThickness = 0.0f;
        _middleLayerMaximamThickness= 1000.0f;
        _bottomLayerIOR = new Vector3(1.0f,1.0f,1.0f);
        _bottomLayerKappa =new Vector3(0.0f,0.0f,0.0f);
        SetMaterialParameters();
    }

    private void SetMaterialParameters(){
        captureMaterial.SetFloat("_TopLayerIOR",_topLayerIOR);
        captureMaterial.SetFloat("_MiddleLayerIOR",_middleLayerIOR);
        captureMaterial.SetFloat("_MiddleLayerMinimamThickness",_middleLayerMinimamThickness);
        captureMaterial.SetFloat("_MiddleLayerMaximamThickness",_middleLayerMaximamThickness);
        captureMaterial.SetVector("_BottomLayerIOR",_bottomLayerIOR);
        captureMaterial.SetVector("_BottomLayerKappa",_bottomLayerKappa);
        captureMaterial.SetVector("_BottomLayerBaseColor",_bottomLayerBaseColor);
        captureMaterial.SetVector("_BottomLayerEdgeTint",_bottomLayerEdgeTint);
    }

    private void SetPreviewMaterialParameters(){
        previewMaterial.SetFloat("_MiddleLayerMinimamThickness",_middleLayerMinimamThickness);
        previewMaterial.SetFloat("_MiddleLayerMaximamThickness",_middleLayerMaximamThickness);
        previewMaterial.SetFloat("_MiddleLayerThickness",_previewLayerThickness);
        previewMaterial.SetTexture("_LUT",lutTexture);
    }
    
    static private float n_min(float r){
        return (1.0f - r) / (1.0f + r);
    }
    static private float n_max(float r){
        return (1.0f + Mathf.Sqrt(r)) / (1.0f - Mathf.Sqrt(r));
    }

    static private float rToIOR(float col,float tint){
        return tint * n_min(col) + (1.0f - tint) * n_max(col);
    }

    static private float rToKappa(float col,float ior){
        float nr = (ior + 1.0f) * (ior + 1.0f) * col - (ior - 1.0f) * (ior - 1.0f);
        return Mathf.Sqrt(nr / (1.0f - col));
    }

    static private float getR(float ior,float kappa){
        return ((ior-1.0f) * (ior - 1.0f) + kappa * kappa) / ((ior + 1.0f) * (ior + 1.0f) + kappa * kappa);
    }

    static private float getG(float ior,float kappa){
        float r = getR(ior,kappa);
        return (n_max(r) - ior) / (n_max(r) - n_min(r));
    }

    static private void rgbToIOR(ref Vector3 ior,ref Vector3 kappa,Color basecol,Color tint){

        basecol.r = Mathf.Clamp(basecol.r,0.00001f,0.99999f);
        basecol.g = Mathf.Clamp(basecol.g,0.00001f,0.99999f);
        basecol.b = Mathf.Clamp(basecol.b,0.00001f,0.99999f);
        tint.r = Mathf.Clamp(tint.r,0.00001f,0.99999f);
        tint.g = Mathf.Clamp(tint.g,0.00001f,0.99999f);
        tint.b = Mathf.Clamp(tint.b,0.00001f,0.99999f);

        ior.x = rToIOR(basecol.r,tint.r);
        ior.y = rToIOR(basecol.g,tint.g);
        ior.z = rToIOR(basecol.b,tint.b);

        kappa.x = rToKappa(basecol.r,ior.x);
        kappa.y = rToKappa(basecol.g,ior.y);
        kappa.z = rToKappa(basecol.b,ior.z);
    }
    
    static private void ciorToRGB(Vector3 ior,Vector3 kappa,ref Color basecol,ref Color tint){
        if(ior.x == 1.0f) ior.x = 1.00001f;
        if(ior.y == 1.0f) ior.y = 1.00001f;
        if(ior.z == 1.0f) ior.z = 1.00001f;

        basecol.r = getR(ior.x,kappa.x);
        basecol.g = getR(ior.y,kappa.y);
        basecol.b = getR(ior.z,kappa.z);

        tint.r = getG(ior.x,kappa.x);
        tint.g = getG(ior.y,kappa.y);
        tint.b = getG(ior.z,kappa.z);

        basecol.r = Mathf.Clamp(basecol.r,0.00001f,0.99999f);
        basecol.g = Mathf.Clamp(basecol.g,0.00001f,0.99999f);
        basecol.b = Mathf.Clamp(basecol.b,0.00001f,0.99999f);
        tint.r = Mathf.Clamp(tint.r,0.00001f,0.99999f);
        tint.g = Mathf.Clamp(tint.g,0.00001f,0.99999f);
        tint.b = Mathf.Clamp(tint.b,0.00001f,0.99999f);
    }

}