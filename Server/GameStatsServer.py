from flask import Flask, request, send_file, abort
import os
import xml.etree.ElementTree as ET

app = Flask(__name__)
SERVER_XML_PATH = 'server_history.xml'  # 云端唯一存储文件

# 文件校验函数（保留格式验证）
def validate_xml_format(xml_content):
    """验证XML是否符合历史记录格式"""
    try:
        root = ET.fromstring(xml_content)
        return root.tag == 'ArrayOfGamePlayData'
    except Exception as e:
        return False

# 简化的上传端点（移除鉴权）
@app.route('/api/history', methods=['PUT'])
def upload_history():
    """
    接收并覆盖云端历史文件
    请求要求：
    - Content-Type: application/xml
    - 有效的XML格式
    （安全由防火墙IP白名单保障）
    """
    if not request.content_type.startswith('application/xml'):
        return {'error': 'Invalid content type'}, 400
    
    xml_content = request.data
    
    # 格式验证（保持数据完整性检查）
    if not validate_xml_format(xml_content):
        return {'error': 'Invalid XML format'}, 400
    
    # 保存文件（直接覆盖）
    try:
        with open(SERVER_XML_PATH, 'wb') as f:
            f.write(xml_content)
        return {'message': 'File updated successfully'}, 200
    except Exception as e:
        return {'error': str(e)}, 500

# 简化的下载端点
@app.route('/api/history', methods=['GET'])
def download_history():
    """获取云端历史文件（无需认证）"""
    if not os.path.exists(SERVER_XML_PATH):
        abort(404, description="History file not found")
    
    return send_file(
        SERVER_XML_PATH,
        mimetype='application/xml',
        as_attachment=False
    )

# 合并端点保持不变（逻辑与安全无关）
@app.route('/api/history/merge', methods=['POST'])
def merge_history():
    if not request.content_type.startswith('application/xml'):
        return {'error': 'Invalid content type'}, 400
    
    # 加载云端数据
    server_root = ET.parse(SERVER_XML_PATH).getroot()
    # 解析客户端数据
    client_root = ET.fromstring(request.data)
    
    # 创建时间索引字典
    existing_records = {
        (elem.find('GameName').text, elem.find('StartTime').text): elem
        for elem in server_root.findall('GamePlayData')
    }
    
    # 合并逻辑
    for client_elem in client_root.findall('GamePlayData'):
        key = (
            client_elem.find('GameName').text,
            client_elem.find('StartTime').text
        )
        
        # 如果不存在相同记录则添加
        if key not in existing_records:
            new_elem = ET.Element('GamePlayData')
            for child in client_elem:
                new_elem.append(child.copy())
            server_root.append(new_elem)
    
    # 保存合并结果
    ET.ElementTree(server_root).write(SERVER_XML_PATH)
    
    return {'message': 'Merge completed', 'count': len(server_root)}, 200

if __name__ == '__main__':
    # 初始化空文件（如果不存在）
    if not os.path.exists(SERVER_XML_PATH):
        ET.ElementTree(ET.Element('ArrayOfGamePlayData')).write(SERVER_XML_PATH)
    app.run(host='0.0.0.0', port=5000)