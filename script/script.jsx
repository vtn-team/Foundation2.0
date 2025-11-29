/*
    After Effects Animation Exporter for Unity (Manual JSON Builder - V4)
    - Adds an "alpha" property based on layer in/out points.
    - ES3 compatible key iteration.
    - All property values are exported as arrays for consistency.
*/

(function main() {

    var comp = app.project.activeItem;
    if (!comp || !(comp instanceof CompItem)) {
        alert("Please select a composition first.");
        return;
    }

    var settings = {
        width: comp.width,
        height: comp.height,
        frameRate: comp.frameRate,
        durationFrames: Math.round(comp.duration * comp.frameRate)
    };

    var layersData = [];
    for (var i = 1; i <= comp.numLayers; i++) {
        var layer = comp.layer(i);
        
        if (!(layer instanceof AVLayer)) continue;

        var layerInfo = {
            name: layer.name,
            source: (layer.source && layer.source.file) ? layer.source.file.name.replace(/\\/g, '\\\\') : "[No Source]",
            sourceWidth: (layer.source) ? layer.source.width : 0,
            sourceHeight: (layer.source) ? layer.source.height : 0,
            properties: {}
        };

        var props = {
            "position": layer.transform.position,
            "scale": layer.transform.scale,
            "rotation": layer.transform.rotation,
            "anchorPoint": layer.transform.anchorPoint
        };

        for (var key in props) {
            var prop = props[key];
            var propData = [];

            if (prop.numKeys > 0) {
                for (var j = 1; j <= prop.numKeys; j++) {
                    propData.push({ frame: Math.round(prop.keyTime(j) * comp.frameRate), value: prop.keyValue(j) });
                }
            } else {
                 propData.push({ frame: 0, value: prop.value });
            }
            layerInfo.properties[key] = propData;
        }

        // ★★★ ここから追加 ★★★
        // レイヤーの表示期間に基づいてalphaプロパティを生成
        var alphaData = [];
        var inFrame = Math.round(layer.inPoint * comp.frameRate);
        var outFrame = Math.round(layer.outPoint * comp.frameRate);

        if (inFrame > 0) {
            // 開始フレームの1フレーム前でalpha=0
            alphaData.push({ frame: inFrame - 1, value: 0 });
        }
        // 開始フレームでalpha=1
        alphaData.push({ frame: inFrame, value: 1 });
        // 終了フレームでalpha=0
        alphaData.push({ frame: outFrame, value: 0 });
        
        layerInfo.properties["alpha"] = alphaData;
        // ★★★ ここまで追加 ★★★

        layersData.push(layerInfo);
    }

    // --- 3. 手動でJSON文字列を組み立てる ---
    // (このセクションは変更なしでOK)
    function buildJsonString() {
        // ... (前回の回答の buildJsonString 関数をそのままここにペースト)
        var s = '{\n';
        s += '\t"setting": {\n';
        s += '\t\t"width": ' + settings.width + ',\n';
        s += '\t\t"height": ' + settings.height + ',\n';
        s += '\t\t"frameRate": ' + settings.frameRate + ',\n';
        s += '\t\t"durationFrames": ' + settings.durationFrames + '\n';
        s += '\t},\n';
        
        s += '\t"layers": [\n';
        for (var i = 0; i < layersData.length; i++) {
            var layer = layersData[i];
            s += '\t\t{\n';
            s += '\t\t\t"name": "' + layer.name + '",\n';
            s += '\t\t\t"source": "' + layer.source + '",\n';
            s += '\t\t\t"sourceWidth": ' + layer.sourceWidth + ',\n';
            s += '\t\t\t"sourceHeight": ' + layer.sourceHeight + ',\n';
            s += '\t\t\t"properties": {\n';
            
            var propCount = 0;
            var totalProps = 0;
            for(var tempKey in layer.properties) { if(layer.properties.hasOwnProperty(tempKey)) totalProps++; }

            for(var key in layer.properties){
                if(layer.properties.hasOwnProperty(key)){
                    propCount++;
                    s += '\t\t\t\t"' + key + '": [\n';
                    for(var k = 0; k < layer.properties[key].length; k++){
                        var keyframe = layer.properties[key][k];
                        s += '\t\t\t\t\t{\n';
                        s += '\t\t\t\t\t\t"frame": ' + keyframe.frame + ',\n';
                        
                        if(typeof keyframe.value === 'object'){
                            s += '\t\t\t\t\t\t"value": [' + keyframe.value.join(',') + ']\n';
                        } else {
                             s += '\t\t\t\t\t\t"value": [' + keyframe.value + ']\n';
                        }
                        
                        s += '\t\t\t\t\t}';
                        if(k < layer.properties[key].length - 1) s += ',';
                        s += '\n';
                    }
                    s += '\t\t\t\t]';
                    if(propCount < totalProps) s += ',';
                    s += '\n';
                }
            }
            s += '\t\t\t}\n';
            s += '\t\t}';
            if (i < layersData.length - 1) s += ',';
            s += '\n';
        }
        s += '\t]\n';
        s += '}';
        return s;
    }
    
    var jsonString = buildJsonString();

    var file = File.saveDialog("Save Animation JSON", "*.json");
    if (file) {
        file.open("w");
        file.encoding = "UTF-8";
        file.write(jsonString);
        file.close();
        alert("Export successful!");
    }

})();