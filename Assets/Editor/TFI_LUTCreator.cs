using UnityEditor;
using UnityEngine;
public class TFI_LUTCreator : EditorWindow
{
    private Camera _captureCamera;
    private GameObject _cameraObject;
    private GameObject _lutPlaneObject;
    private Material _lutCaptureMaterial;
    private RenderTexture _lutRenderTexture;
    private Texture2D _lutTexture;
    private GameObject _previewObject;
    private Material _previewMaterial;

    //Window Scroll
    private Vector2 _scrollPos;

    //Texture Parameters
    private int _lutTextureSize = 1024;
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
    private string[] _exampleMaterialNames ;
    private Vector3[] _exampleMaterialIOR;
    private Vector3[] _exampleMaterialCoefficients;

    //Color
    private Color _bottomLayerBaseColor = Color.white;
    private Color _bottomLayerEdgeTint = Color.white;

    //Popup
    private int _selectedOption = 0;
    private string[] _options = new string[] {"Dielectric IOR","Color Control Metallic CIRO","Direct control CIOR"};

    [MenuItem("Window/TFI_LUTCreator")]
    private static void ShowWindow()
    {
        TFI_LUTCreator window = GetWindow<TFI_LUTCreator>();
        window.titleContent = new GUIContent("TFI_LUTCreator");
        window.Show();

    }

    private void OnEnable()
    {
        _exampleMaterialIOR = new Vector3[]{ 
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

        _exampleMaterialCoefficients =  new Vector3[]{
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
        
        _exampleMaterialNames = new string[]{
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
        var findTFIcapture = GameObject.Find("TFI_LUTCaptureCamera");
        var findTFIpreview = GameObject.Find("TFI_LUTPreviewCamera");
        var findTFIplane = GameObject.Find("TFI_LUTPlane");
        var findTFI_previewObject = GameObject.Find("TFI_LUTPreviewObject");

        DestroyImmediate(findTFIcapture);
        DestroyImmediate(findTFIplane);
        DestroyImmediate(findTFIpreview);
        DestroyImmediate(findTFI_previewObject);
        
        DestroyImmediate(_lutCaptureMaterial);
        DestroyImmediate(_previewMaterial);
        DestroyImmediate(_lutTexture);
        DestroyImmediate(_lutRenderTexture);
        DestroyImmediate(_previewRenderTexture);
        DestroyImmediate(_previewTexture);
    }
    private void OnGUI()
    {
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
        // _captureCamera = EditorGUILayout.ObjectField("LUT Capture Camera", _captureCamera, typeof(Camera), true) as Camera;
        // _lutPlaneObject = EditorGUILayout.ObjectField("LUT Plane", _lutPlaneObject, typeof(GameObject), true) as GameObject;
        // _lutCaptureMaterial = EditorGUILayout.ObjectField("Capture Material", _lutCaptureMaterial, typeof(Material), true) as Material;


        GUILayout.Space(10);
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Initialize", GUILayout.Height(100),GUILayout.Width(100)))
            {
                CreateLUTCaptureObjects();
            }
            if (GUILayout.Button("Capture LUT",GUILayout.Height(100),GUILayout.Width(100)))
            {
                if (_captureCamera != null && _lutPlaneObject != null && _lutCaptureMaterial != null)
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
        EditorGUI.DrawPreviewTexture(new Rect(50,rect.y + rect.height + 10, 256, 256), _lutTexture);        
        EditorGUI.DrawPreviewTexture(new Rect(70 + 256,rect.y + rect.height + 10, 256, 256), _previewTexture);        

        GUILayout.Space(256 + 30);

        _foldPreview = EditorGUILayout.Foldout(_foldPreview,"Preview Setting");
        if(_foldPreview){
            // _previewObject = EditorGUILayout.ObjectField("LUT Preview Object", _previewObject, typeof(GameObject), true) as GameObject;
            // _previewMaterial = EditorGUILayout.ObjectField("LUT Preview Material", _previewMaterial, typeof(Material), true) as Material;
            // _previewCamera = EditorGUILayout.ObjectField("Preview Camera", _previewCamera, typeof(Camera), true) as Camera;
            _previewLayerThickness = EditorGUILayout.FloatField("Layer Thickness",_previewLayerThickness);
        }

        GUILayout.Space(20);

        //Texture Setting
        _foldOutTexture = EditorGUILayout.Foldout(_foldOutTexture,"Texture Setting");
        if(_foldOutTexture){
            _textureSizeOp = EditorGUILayout.Popup("Texture Size", _textureSizeOp, _textureSizeOptions); 
            switch(_textureSizeOp){
                case 0:
                    _lutTextureSize = 32;
                    break;
                case 1:
                    _lutTextureSize = 64;
                    break;
                case 2:
                    _lutTextureSize = 128;
                    break;
                case 3:
                    _lutTextureSize = 256;
                    break;
                case 4:
                    _lutTextureSize = 512;
                    break;
                case 5:
                    _lutTextureSize = 1024;
                    break;
                case 6:
                    _lutTextureSize = 2048;
                    break;
                case 7:
                    _lutTextureSize = 4096;
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

                for (int i = 0; i < _exampleMaterialNames.Length; i++)
                {
                    DrawTableRow(_exampleMaterialNames[i], _exampleMaterialIOR[i], _exampleMaterialCoefficients[i]); // テーブルの行を描画
                }
            }
            

        }

        GUILayout.Space(20);


        GUILayout.Space(20);
        EditorGUILayout.EndScrollView();

        if(_lutCaptureMaterial != null){
            SetMaterialParameters();
        }
        if(_previewMaterial != null){
            SetPreviewMaterialParameters();
        }
        
        if(_previewCamera != null){
            PreviewRender();
        }
        if(_captureCamera != null){
            RenderPreviewLUT();
        }
    }
    
    private void setTextureResolution(int texSize){
        _lutRenderTexture = new RenderTexture(texSize,texSize,0,RenderTextureFormat.ARGB32); 
        _lutRenderTexture.filterMode = FilterMode.Bilinear;
        _lutRenderTexture.Create();
        _lutTexture = new Texture2D(texSize,texSize,TextureFormat.ARGB32,false);
        _lutTexture.filterMode = FilterMode.Bilinear;
        _lutTexture.wrapMode = TextureWrapMode.Clamp;

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
        RenderTexture.active = _lutRenderTexture;
        _captureCamera.targetTexture = _lutRenderTexture;

        _captureCamera.Render();

        _lutTexture.ReadPixels(new Rect(0, 0, _lutTextureSize, _lutTextureSize), 0, 0);
        _lutTexture.Apply();

        RenderTexture.active = null;

    }
    private void RenderLUT(){
        setTextureResolution(_lutTextureSize);
        RenderTexture.active = _lutRenderTexture;
        _captureCamera.targetTexture = _lutRenderTexture;

        _captureCamera.Render();

        _lutTexture.ReadPixels(new Rect(0, 0, _lutTextureSize, _lutTextureSize), 0, 0);
        _lutTexture.Apply();

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

        byte[] bytes = _lutTexture.EncodeToPNG();
        System.IO.File.WriteAllBytes(filePath, bytes);
        AssetDatabase.Refresh();

        Debug.Log("Texture Size: " + _lutTexture.width.ToString() + "x" + _lutTexture.height.ToString());
        Debug.Log("MiddleLapyerIOR: " + _middleLayerIOR.ToString());
        Debug.Log("MiddleLayerThickness: " + _middleLayerMinimamThickness.ToString() + " ~ " + _middleLayerMaximamThickness.ToString());
        Debug.Log("BottomLayerIOR: " + _bottomLayerIOR.ToString());
        Debug.Log("BottomLayerKappa: " + _bottomLayerKappa.ToString());
        Debug.Log("LUT saved to: " + filePath);
    }

    private void CreateLUTCaptureObjects()
    {
        var findcamera = GameObject.Find("TFI_LUTCaptureCamera");
        var findplane = GameObject.Find("TFI_LUTPlane");

        if (findcamera == null)
        {
            GameObject _captureCameraObj = new GameObject("TFI_LUTCaptureCamera");
            _cameraObject = _captureCameraObj;
            _captureCamera = _captureCameraObj.AddComponent<Camera>();
        }
        else{
            _cameraObject = findcamera;
            _captureCamera = findcamera.GetComponent<Camera>();
        }

        if(findplane == null){
            GameObject planeObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _lutPlaneObject = planeObj;
        }
        else{
            _lutPlaneObject = findplane;
        }

        if(_lutCaptureMaterial == null){
            _lutCaptureMaterial = new Material(Shader.Find("TFI_LUTCreator/InterferenceLUT"));
        }

        int LUTlayer = 8;
        _lutPlaneObject.name = "TFI_LUTPlane";
        _lutPlaneObject.GetComponent<MeshRenderer>().receiveShadows = false;
        _lutPlaneObject.GetComponent<MeshRenderer>().sharedMaterial = _lutCaptureMaterial;
        _lutPlaneObject.layer = LUTlayer;

        _lutPlaneObject.transform.position = new Vector3(0, 0, 0);
        _lutPlaneObject.transform.rotation = Quaternion.Euler(0, 0, 0);
        _lutPlaneObject.transform.localScale = new Vector3(1,1,1);

        _cameraObject.transform.position = new Vector3(0, 1, 0);
        _cameraObject.transform.rotation = Quaternion.Euler(90,180, 0);
        _cameraObject.transform.localScale = new Vector3(1,1,1);

        _captureCamera.orthographic = true;
        _captureCamera.cullingMask = 1 << LUTlayer;

        if(_previewMaterial == null){
            _previewMaterial = new Material(Shader.Find("TFI_LUTCreator/LUT_Viewer"));
        }

        var findPreviewObj = GameObject.Find("TFI_LUTPreviewObject");
        if(findPreviewObj == null){
            _previewObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        }
        else{
            _previewObject = findPreviewObj;
        }

        _previewObject.name = "TFI_LUTPreviewObject";
        _previewObject.GetComponent<MeshRenderer>().receiveShadows = false;
        _previewObject.GetComponent<MeshRenderer>().sharedMaterial = _previewMaterial;
        _previewObject.transform.position = new Vector3(8, 0, 0);
        _previewObject.transform.localScale = new Vector3(4,4,4);

        var findPreviewCamera = GameObject.Find("TFI_LUTPreviewCamera");
        if(findPreviewCamera == null){
            _previewCameraObject = new GameObject("TFI_LUTPreviewCamera");
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
        _lutCaptureMaterial.SetFloat("_TopLayerIOR",_topLayerIOR);
        _lutCaptureMaterial.SetFloat("_MiddleLayerIOR",_middleLayerIOR);
        _lutCaptureMaterial.SetFloat("_MiddleLayerMinimamThickness",_middleLayerMinimamThickness);
        _lutCaptureMaterial.SetFloat("_MiddleLayerMaximamThickness",_middleLayerMaximamThickness);
        _lutCaptureMaterial.SetVector("_BottomLayerIOR",_bottomLayerIOR);
        _lutCaptureMaterial.SetVector("_BottomLayerKappa",_bottomLayerKappa);
        _lutCaptureMaterial.SetVector("_BottomLayerBaseColor",_bottomLayerBaseColor);
        _lutCaptureMaterial.SetVector("_BottomLayerEdgeTint",_bottomLayerEdgeTint);
    }

    private void SetPreviewMaterialParameters(){
        _previewMaterial.SetFloat("_MiddleLayerMinimamThickness",_middleLayerMinimamThickness);
        _previewMaterial.SetFloat("_MiddleLayerMaximamThickness",_middleLayerMaximamThickness);
        _previewMaterial.SetFloat("_MiddleLayerThickness",_previewLayerThickness);
        _previewMaterial.SetTexture("_LUT",_lutTexture);
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