﻿namespace UnityLineGraph
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.UI;
    using RSToolkit.Controls;

    public class LineGraphController : MonoBehaviour
    {
        [SerializeField]
        private Sprite dotSprite;
        [SerializeField]
        private Font font;


        // グラフを表示する範囲
        private RectTransform viewport;
        // グラフの要素を配置するContent
        // グラフの要素はグラフの点、ライン
        private RectTransform content;
        public Spawner YmarkerContent;
        public Spawner XmarkerContent;
        // 軸のGameObject
        private GameObject xUnitLabel;
        private GameObject yUnitLabel;

        private GameObject previousDot;

        public List<GraphLine> GraphLines{
            get{
                return m_GraphLineSpawner.SpawnedGameObjects.Select(gl => gl.GetComponent<GraphLine>()).ToList();
            }
        }

        public float xPixelsPerUnit = 50f;
        public float yPixelsPerUnit = 5f;
        public float yPixelsSecondValuePerUnit{ get; private set; } = 5f;

        public float GetyPixelsPerUnit(bool isSecondValue = false){ 
            return isSecondValue ? yPixelsSecondValuePerUnit : yPixelsPerUnit;
        }
        public float yAxisUnitSpan = 10f;

        public float yAxisSecondValueUnitSpan = 0f;

        public float GraphLineYOffset = 0f;
        public float GraphLineYSecondValueOffset = 0f;

        public float GetyAxisUnitSpan(bool isSecondValue = false){
            return isSecondValue ? yAxisSecondValueUnitSpan : yAxisUnitSpan;
        }

        public float GetGraphLineYOffset(bool isSecondValue){
            return isSecondValue ? GraphLineYSecondValueOffset : GraphLineYOffset;
        }

        public bool AutoScroll = true;
        public float SeperatorThickness = 2f;
        public List<string> xAxisLabels;

        public bool FitYAxisToBounderies = false;
        public bool FitXAxisToBounderies = false;

        public float GetyAxisSepHeight(bool isSecondValue = false){
            return GetyAxisUnitSpan(isSecondValue) * yPixelsPerUnit;
        }

        /*
        private float m_OffsetY = 0f;

        public float OffsetY{
            get{
                return m_OffsetY;
            }
            private set{
                m_OffsetY = value;
            }
        }
        */

        public void ResetSettings(){
            xPixelsPerUnit = 50f;
            yPixelsPerUnit = 5f;
            yAxisUnitSpan = 10f;
            yAxisSecondValueUnitSpan = 0f;
            AutoScroll = true;
            SeperatorThickness = 2f;
        }

        private void Awake()
        {
            viewport = this.transform.Find("Viewport") as RectTransform;
            content = viewport.Find("Content") as RectTransform;
            xUnitLabel = this.transform.Find("X Unit Label").gameObject;
            yUnitLabel = this.transform.Find("Y Unit Label").gameObject;
        }

        /// <summary>
        /// X軸方向の単位を設定
        /// </summary>
        /// <param name="text">Text.</param>
        public void SetXUnitText(string text)
        {
            if(xUnitLabel != null){
                xUnitLabel.GetComponent<Text>().text = text;
            }
        }

        /// <summary>
        /// Y軸方向の単位を設定
        /// </summary>
        /// <param name="text">Text.</param>
        public void SetYUnitText(string text)
        {
            if(yUnitLabel != null){
                yUnitLabel.GetComponent<Text>().text = text;
            }
        }

        private MultiSpawner m_graphLineSpawner;
        public MultiSpawner m_GraphLineSpawner{
            get{
                if(m_graphLineSpawner == null){
                    m_graphLineSpawner = content.GetComponent<MultiSpawner>();
                }
                return m_graphLineSpawner;
            }
        }

        public GraphLine AddGraphLine(int lineIndex = 0){
            var graphLine = m_GraphLineSpawner.SpawnAndGetGameObject(lineIndex).GetComponent<GraphLine>();
            graphLine.parentController = this;
            graphLine.SetDefaultLRTB();
            return graphLine;
        }
        public GraphLine AddGraphLine(Color color, int lineIndex = 0){
            return AddGraphLine(color, color, lineIndex);
        }
        public GraphLine AddGraphLine(Color lineColor, Color pointColor, int lineIndex = 0){
            var graphLine = AddGraphLine(lineIndex);
            graphLine.SetColors(lineColor, pointColor);
            return graphLine;
        }

        /// <summary>
        /// グラフがスクロールされた時の処理
        /// </summary>
        /// <param name="scrollPosition">スクロールの位置</param>
        public void OnGraphScroll(Vector2 scrollPosition)
        {
            UpdateMakersPosition();
        }

        /// <summary>
        /// Contentのサイズを調整する
        /// </summary>
        private void ResizeContentContainer()
        {
            Vector2 buffer = new Vector2(10, 10);
            ///float width = (Settings.xAxisLabels.Count / 2) * Settings.xSize;
            float width = (xAxisLabels.Count + 1) * xPixelsPerUnit;
            int sepCount = YmarkerContent.SpawnedGameObjects.Count;
            /*
            float height = (yAxisUnitSpan * sepCount * yPixelsPerUnit)
                            - ((yAxisUnitSpan / 4) * yPixelsPerUnit)
                            + SeperatorThickness;
            */
            float height = (yAxisUnitSpan * sepCount * yPixelsPerUnit);
            if(FitYAxisToBounderies){
                height = viewport.rect.height;
            }

            content.sizeDelta = new Vector2(width, height) + buffer;
            YmarkerContent.GetComponent<RectTransform>().sizeDelta = new Vector2(YmarkerContent.GetComponent<RectTransform>().sizeDelta.x, content.sizeDelta.y);
        }

        /// <summary>
        /// 現在の最大値を取得する
        /// </summary>
        /// <returns>最大値</returns>
        private float GetMaxY()
        {
            float max = float.MinValue;

            for (int i = 0; i < GraphLines.Count; i++){
                max = Mathf.Max(max, GraphLines[i].GetMaxY());
            }

            return max;
        }
        /*
        private float GetSepMaxY(){
            float sepMax = float.MinValue;

            for (int i = 0; i < GraphLines.Count; i++){
                sepMax = Mathf.Max(sepMax, GraphLines[i].GetSepMaxY());
            }

            return sepMax;
        }
        */
        private float? GetSepMaxY(bool isSecondValue = false){
            return GetSepMaxY(GraphLines.Where(gl => gl.IsSecondValue == isSecondValue).ToList());
        }
        private float? GetSepMaxY(List<GraphLine> graphLines){
            float sepMax = float.MinValue;
            var gls = graphLines.Where(gl => gl.GetSepMaxY() != null).ToList();
            if(!gls.Any()){
                return null;
            }

            for (int i = 0; i < gls.Count; i++){
                sepMax = Mathf.Max(sepMax, gls[i].GetSepMaxY().Value);
            }

            return sepMax;
        }

        /*
        private float GetMinY(){
            float min = float.MaxValue;
            for (int i = 0; i < GraphLines.Count; i++){
                if(GraphLines[i].ValueCount > 0){
                    min = Mathf.Min(min, GraphLines[i].GetMinY());
                }
            }
            if(min == float.MaxValue){
                min = 0;
            }

            return min;
        }
        */

        private float GetMinY(bool isSecondValue = false) {
            return GetMinY(GraphLines.Where(gl => isSecondValue).ToList());
        }
        private float GetMinY(List<GraphLine> graphLines) {
            float min = float.MaxValue;
            for (int i = 0; i < graphLines.Count; i++){
                if(graphLines[i].ValueCount > 0){
                    min = Mathf.Min(min, graphLines[i].GetMinY());
                }
            }
            if(min == float.MaxValue){
                min = 0;
            }

            return min;
        }

        /*
        private float GetSepMinY(){
            float sepMin = float.MaxValue;

            for (int i = 0; i < GraphLines.Count; i++){
                if(GraphLines[i].ValueCount > 0){
                    sepMin = Mathf.Min(sepMin, GraphLines[i].GetSepMinY());
                }
            }

            if(sepMin == float.MaxValue){
                sepMin = 0;
            }
            return sepMin;
        }
        */
        public float? GetSepMinY(bool isSecondValue = false){
            return GetSepMinY(GraphLines.Where(gl => gl.IsSecondValue == isSecondValue).ToList());
        }
        private float? GetSepMinY(List<GraphLine> graphLines){
            float sepMin = float.MaxValue;
            var gls = graphLines.Where(gl => gl.GetSepMinY() != null).ToList();
            if(!gls.Any()){
                return null;
            }
            for (int i = 0; i < gls.Count; i++){
                if(gls[i].ValueCount > 0){
                    sepMin = Mathf.Min(sepMin, gls[i].GetSepMinY().Value);
                }
            }

            if(sepMin == float.MaxValue){
                sepMin = 0;
            }
            return sepMin;
        }

        /// <summary>
        /// グラフ外のラベルと軸セパレータの位置を更新
        /// </summary>
        private void UpdateMakersPosition()
        {
            Vector2 contentPosition = content.anchoredPosition;
            YmarkerContent.GetComponent<RectTransform>().anchoredPosition = new Vector2(YmarkerContent.GetComponent<RectTransform>().anchoredPosition.x, content.anchoredPosition.y + (SeperatorThickness / 2f));
            XmarkerContent.GetComponent<RectTransform>().anchoredPosition = new Vector2(content.anchoredPosition.x,  XmarkerContent.GetComponent<RectTransform>().anchoredPosition.y);

        }

    public void ClearGraph(){
        for(int i = 0; i < GraphLines.Count; i++){
            GraphLines[i].Clear();
        }
        
        XmarkerContent.DestroyAllSpawns();
        YmarkerContent.DestroyAllSpawns();
    } 

        /// <summary>
        /// グラフの表示を更新する
        /// </summary>
        public void RefreshGraphUI()
        {

            // Xセパレータの更新
            CreateXAxisMarkers();
            // Yセパレータの更新
            CreateYAxisMarkers();

            ResizeContentContainer();

            UpdateMakersPosition();

            /*
            float mHeight = 0f;

            // OffsetY = GetSepMinY();
            var m = YmarkerContent.SpawnedGameObjects.FirstOrDefault();
            if (m!= null){
                mHeight = m.GetComponent<RectTransform>().rect.height; // / 2;
            }

            int yMarkerCount = YmarkerContent.SpawnedGameObjects.Count() - 1;
            float offsetY = ((float)yMarkerCount * mHeight) / 2f;
            */

            for(int i = 0; i < GraphLines.Count; i++){
                GraphLines[i].Generate();

                // GraphLines[i].SlideGraphVertically(((float)(YmarkerContent.SpawnedGameObjects.Count() - 1) * mHeight) / 2f);
                GraphLines[i].SlideGraphVertically(GetGraphLineYOffset(GraphLines[i].IsSecondValue)); //offsetY);
            }
            if(GraphLines.Any() && AutoScroll){
                ScrollToPoint(GraphLines[0].EndPoint);
            }
        }

        /// <summary>
        /// ある点をグラフの中央になるようにスクロールする
        /// </summary>
        public void ScrollToPoint(GraphPoint point)
        { 
            if(point == null){
                return;
            } 
            Vector2 viewportSize =
                new Vector2(viewport.rect.width, viewport.rect.height);
            Vector2 contentSize =
                new Vector2(content.rect.width, content.rect.height);

            Vector2 contentPosition = - point.AnchoredPosition + 0.5f * viewportSize;

            if(contentSize.x < viewportSize.x)
            {
                contentPosition.x = 0.0f;
            }
            else
            {
                contentPosition.x = Mathf.Clamp(contentPosition.x, -contentSize.x + viewportSize.x, 0);
            }

            if(contentSize.y < viewportSize.y)
            {
                contentPosition.y = 0.0f;
            }
            else
            {
                contentPosition.y = Mathf.Clamp(contentPosition.y, -contentSize.y + viewportSize.y, 0);
            }

            content.localPosition = contentPosition;
            //YmarkerContent.GetComponent<RectTransform>().anchoredPosition = new Vector2(YmarkerContent.GetComponent<RectTransform>().anchoredPosition.x, content.anchoredPosition.y + Settings.seperatorThickness);
            UpdateMakersPosition();
        }


        #region Markers
        private void CreateXMarker(int index, string labelText){
            var markerName = "XMarker(" + index + ")";
            if(XmarkerContent.SpawnedGameObjects.Any(xmc => xmc.name == markerName)){
                return;
            }

            var marker = XmarkerContent.SpawnAndGetGameObject().GetComponent<Marker>();
            marker.Init(labelText, new Color(0, 0, 0, 0.25f));
            XmarkerContent.GetComponent<RectTransform>().sizeDelta = new Vector2((index + 1) * xPixelsPerUnit, 0);

            marker.name = markerName;
        }

        public void CreateXAxisMarkers(){
            XmarkerContent.DestroyAllSpawns();

            if(FitXAxisToBounderies){
                xPixelsPerUnit = viewport.GetComponent<RectTransform>().rect.width / (xAxisLabels.Count + 1) ;
            }
            for (int x = 0; x < xAxisLabels.Count; x++) // += settings.valueSpan)
            {
                CreateXMarker(x, xAxisLabels[x]);
            }
        }
        /*
        private void CreateYMarker(float y)
        {
            var markerName =  "YMarker(" + y + ")";
            if(YmarkerContent.SpawnedGameObjects.Any(ymc => ymc.name == markerName)){
                return;
            }

            var marker = YmarkerContent.SpawnAndGetGameObject().GetComponent<Marker>();
            marker.Init(y.ToString(), new Color(0, 0, 0, 0.25f));
            marker.transform.SetAsFirstSibling();
            marker.name = markerName;
            //marker.SetLabelText(y.ToString()); 
        }
        */
        private Marker CreateYMarker(float y, float? ySecondVal)
        {
            var markerName = string.Format("YMarker({0})", y);
            if(ySecondVal != null){
                markerName = string.Format("YMarker({0}|{1})", y, ySecondVal);
            }
            /*
            if(YmarkerContent.SpawnedGameObjects.Any(ymc => ymc.name == markerName)){
                return;
            }*/
            var markerGO = YmarkerContent.SpawnedGameObjects.FirstOrDefault(ymc => ymc.name == markerName);
            if(markerGO != null){
                return markerGO.GetComponent<Marker>();
            }

            var marker = YmarkerContent.SpawnAndGetGameObject().GetComponent<Marker>();
            if(ySecondVal != null){
                marker.Init(y.ToString(), new Color(0, 0, 0, 0.25f), ySecondVal.Value.ToString());
            }else{
                marker.Init(y.ToString(), new Color(0, 0, 0, 0.25f));
            }

            marker.transform.SetAsFirstSibling();
            marker.name = markerName;
            //marker.SetLabelText(y.ToString()); 
            return marker;
        }

        public Marker GetXMarker(float x){

            GameObject result = YmarkerContent.SpawnedGameObjects.Where(m => m.GetComponent<Marker>().HasLabelText(x.ToString())).FirstOrDefault();
            if(result != null){
                return result.GetComponent<Marker>();
            }
            return null;
        }

        public Marker GetYMarker(float y, float? ySecondVal){

            GameObject result;
            if(ySecondVal == null){
                result = YmarkerContent.SpawnedGameObjects.Where(m => m.GetComponent<Marker>().HasLabelText(y.ToString())).FirstOrDefault();
            }else{
                result = YmarkerContent.SpawnedGameObjects.Where(m => m.GetComponent<Marker>().HasLabelText(y.ToString())
                                                                && m.GetComponent<Marker>().HasSecondValueLabelText(ySecondVal.Value.ToString())
                                                                ).FirstOrDefault();
            }
            if(result != null){
                return result.GetComponent<Marker>();
            }
            return null;
        }

        private int GetSepCount(bool isSecondValue){
            float? sepMaxValue = GetSepMaxY(isSecondValue);
            float? sepMinValue = GetSepMinY(isSecondValue);
            if(sepMaxValue == null || sepMinValue == null){
                return 0;
            }

            int minSepCount = Mathf.CeilToInt(viewport.rect.height / GetyAxisSepHeight(isSecondValue));  

            int result = Mathf.CeilToInt((sepMaxValue.Value - sepMinValue.Value) / GetyAxisUnitSpan(isSecondValue));
            if (!FitYAxisToBounderies){
                result = Mathf.Max(minSepCount, result);
            }

            return result;
        }

        public int GetSepCount(){
            int val1SepCount = GetSepCount(false);
            int result = val1SepCount;

            if(yAxisSecondValueUnitSpan > 0){
                int val2SepCount = GetSepCount(true);
                if(val1SepCount < val2SepCount){
                    result = val2SepCount;
                }
            }

            if (result > 0 && FitYAxisToBounderies){
                yPixelsPerUnit = (viewport.rect.height / (float)result) / yAxisUnitSpan;
                yPixelsSecondValuePerUnit = (viewport.rect.height / (float)result) / yAxisSecondValueUnitSpan;
            }

            return result;
        }

        /*
        /// <summary>
        /// Y軸のセパレータを今のグラフに合わせて表示する
        /// </summary>
        private void CreateYAxisMarkers()
        {
            YmarkerContent.DestroyAllSpawns();

            float sepMaxValue = GetSepMaxY();
            float sepMinValue = GetSepMinY();


            int minSepCount = Mathf.CeilToInt(viewport.rect.height / yAxisSepHeight);  
            int sepCount = Mathf.CeilToInt((sepMaxValue - sepMinValue) / yAxisUnitSpan);
            if (FitYAxisToBounderies){
                yPixelsPerUnit = (viewport.rect.height / sepCount) / yAxisUnitSpan;
            }else{
                sepCount = Mathf.Max(minSepCount, sepCount);
            }

            for(int i = 0; i < sepCount; i ++ )
            {
                float y = sepMinValue + (i * yAxisUnitSpan);
                string markerName = "YMarker(" + y + ")";
                var yMarker = YmarkerContent.transform.Find(markerName);

                // 存在したら追加しない
                if (yMarker == null)
                {
                    CreateYMarker(y);
                }else{
                    yMarker.GetComponent<Marker>().SetLabelText(y.ToString()); 
                }
            }
        }
        */

        /// <summary>
        /// Y軸のセパレータを今のグラフに合わせて表示する
        /// </summary>
        private void CreateYAxisMarkers()
        {
            YmarkerContent.DestroyAllSpawns();


            float? sepMinY = GetSepMinY();
            float? sepVal2MinY = null;

            if(yAxisSecondValueUnitSpan > 0 ){
                sepVal2MinY = GetSepMinY(true);
            }

            int sepCount = GetSepCount();

            var hasVal1 = sepMinY != null; // GraphLines.Any(gl => gl.IsSecondValue == false && gl.HasValues());
            var hasVal2 = sepVal2MinY != null; // GraphLines.Any(gl => gl.IsSecondValue == true && gl.HasValues());

            for(int i = 0; i < sepCount; i ++ )
            {
                //float y = sepMinValue + (i * yAxisUnitSpan);
                float y = sepMinY.Value + (i * yAxisUnitSpan);
                float? ySecondVal = null;
                if(yAxisSecondValueUnitSpan > 0 ){
                    ySecondVal = sepVal2MinY + (i * yAxisSecondValueUnitSpan);
                }
                string markerName = string.Format("YMarker({0})", y);
                if(ySecondVal != null){
                    markerName = string.Format("YMarker({0}|{1})", y, ySecondVal);
                }
                var yMarkerGO = YmarkerContent.transform.Find(markerName);
                string yLabel = string.Empty;
                string y2Label = string.Empty;
                if (hasVal1){
                    yLabel = y.ToString();
                }
                if(hasVal2){
                    y2Label = ySecondVal.ToString();
                }
                Marker yMarker;
                // 存在したら追加しない
                if (yMarkerGO == null)
                {
                    yMarker = CreateYMarker(y, ySecondVal);
                }else{
                    yMarker = yMarkerGO.GetComponent<Marker>();
                }
                yMarker.GetComponent<Marker>().SetLabelText(yLabel); 
                yMarker.GetComponent<Marker>().SecondValueLabel(y2Label);
                //}
            }
        }

/*
        public float GetYMarkerSpacing(){
           if(YmarkerContent.SpawnedGameObjects.Any()){
               return Mathf.Abs(YmarkerContent.SpawnedGameObjects.Last()
                                .GetComponent<Marker>().GetPosition().y);
           }
           return 0f;
        }

        public float GetYposUsingYMarkers(float yVal, bool isSecondValue = false){
            for(int i = 0; i < YmarkerContent.SpawnedGameObjects.Count - 1; i = i+2){
               var m =  YmarkerContent.SpawnedGameObjects[i].GetComponent<Marker>();
               var m2 =  YmarkerContent.SpawnedGameObjects[i+1].GetComponent<Marker>();
               float m_val = 0f; 
               float m2_val = 0f; 
               if(isSecondValue){
                   m_val = float.Parse(m.GetSecondValueLabelText());
                   m2_val = float.Parse(m2.GetSecondValueLabelText());
               }else{
                   m_val = float.Parse(m.GetLabelText());
                   m2_val = float.Parse(m2.GetLabelText());
               }
               if(yVal >= m_val && yVal <= m2_val){
                   var val_pos_y = m.GetPosition(true);
                   return (yVal * val_pos_y.y) / m_val;
               }
            }

            return 0f;
        }
*/

        #endregion

    }
}
