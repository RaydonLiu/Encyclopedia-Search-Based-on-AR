from werkzeug.utils import secure_filename
from flask import Flask, render_template, jsonify, request
import time
import os

import wikipedia as wiki
from detector_core.notebooks.model_start import process_image

from nets import ssd_vgg_300, ssd_common, np_methods
from preprocessing import ssd_vgg_preprocessing
from notebooks import visualization

import matplotlib.pyplot as plt
import matplotlib.image as mpimg

app = Flask(__name__)
UPLOAD_FOLDER = 'upload'
app.config['UPLOAD_FOLDER'] = UPLOAD_FOLDER
basedir = os.path.abspath(os.path.dirname(__file__))
ALLOWED_EXTENSIONS = set(['txt', 'png', 'jpg', 'xls', 'JPG', 'PNG', 'xlsx', 'gif', 'GIF'])

wiki.set_lang('zh-tw')

dataClass = {'1': '飞机',
             '2': '自行车',
             '3': '鸟',
             '4': '船',
             '5': '瓶子',
             '6': '巴士',
             '7': '汽车',
             '8': '猫',
             '9': '椅子',
             '10': '奶牛',
             '11': '桌子',
             '12': '狗',
             '13': '马',
             '14': '摩托车',
             '15': '人',
             '16': '植物',
             '17': '绵羊',
             '18': '沙发',
             '19': '火车',
             '20': '显示器'}

wikiInfor = {
    "飞机": "固定翼飞机（Fixed-wing aeroplane）简称定翼机,常被再简称为飞机（英文:aeroplane,airplane）,是指由动力装置产生前进的推力或拉力,由机身的固定机翼产生升力,在大气层内飞行的重于空气的航空器。",
    "自行车": "自行车,或称脚踏车、单车、自由车、铁马,是一种以人力踩踏脚踏板驱动的小型陆上车辆。",
    "鸟": "鸟类是两足、恒温、卵生的脊椎动物,身披羽毛,前肢演化成翅膀,具有坚硬的喙。",
    "船": "船或船舶,指的是:举凡利用水的浮力,依靠人力、风帆、发动机（如蒸气机、燃气涡轮、柴油引擎、核子动力机组）等动力,牵、拉、推、划、或推动螺旋桨、高压喷嘴,使能在水上移动的交通运输手段。",
    "瓶子": "瓶子一般指口部比腹部窄小、颈长的容器,多数由陶瓷、玻璃、塑料或铝等不容易渗漏的物料制造。",
    "巴士": "公共汽车是一种以中型以运输大量乘客的公交服务。",
    "汽车": "汽车或称机动车（英式英语:car；美式英语:automobile；美国口语:auto）,即本身具有动力得以驱动,不须依轨道或电缆,得以动力行驶之车辆。",
    "猫": "猫（学名:Felis silvestris catus）,通常指家猫,在现代汉语中多称猫咪,为小型猫科动物,是为野猫（又称斑猫；Felis silvestris）中的亚种,此外也有其它未经过《国际动物命名法规》认可的命名,例如Felis catus。",
    "椅子": "椅,无靠背的称为凳,是一件用来坐的家具,为脚物家具的一种,一般包括一个座位、椅背,有时还包括扶手,通常会有椅脚使座位高于地面。",
    "奶牛": "乳牛（也称奶牛）是专门培养出来产牛奶的母牛。",
    "桌子": "桌,又称台,是家具的一种,由一个平面及支架组成,用作盛载物件之用。",
    "狗": "犬（学名:Canis lupus familiaris）,现代俗称为狗,一种常见的犬科哺乳动物,与狼为同一种动物,生物学分类上是狼的一个亚种。",
    "马": "马（学名:Equus ferus caballus）,是一种草食性家畜,广泛分布于世界各地,原产于中亚草原,6000多年前就被人类驯养,最早的马匹驯养遗址于乌克兰草原发现,15世纪后,才被欧洲殖民者带到美洲和澳大利亚地区。",
    "摩托车": "摩托车（来自英语的「Motorcycle」或「Motorbike」），在台湾另称为摩托车、Autobike（台湾话）、机器脚踏车，在香港和澳门称为电单车，东南亚华人圈则称为摩哆，是指两轮或三轮的机动车辆，由摩托化自行车衍化而来，以两轮为大宗。",
    "人": "现代人在生物学上属灵长目、人科、人属、智人种,由人猿／古猿演化而来。",
    "植物": "植物（Plantae）是生命的主要形态之一,并包含了如乔木、灌木、藤类、青草、蕨类及绿藻等熟悉的生物。",
    "绵羊": "绵羊（英语:Sheep,学名:Ovis aries）亦称为家羊或白羊,属哺乳纲偶蹄目牛科羊亚科,是一种四足反刍哺乳动物,也是世界上数量最多的羊种,共计超过十亿。",
    "沙发": "沙发（英语:Sofa,北美称作Couch）为软件家具的一种,是装有软垫的多座位椅子。",
    "火车": "铁路列车,简称列车,或称火车,是指在铁路轨道上行驶的车辆,通常由多节车厢所组成,可以载运乘客或是货物。",
    "显示器": "显示器（英语:display device）,一种输出装置（Output device）,用于显示图像及色彩。"
}


# Use to detect object from a image
def detectFromimage(path):

    objectClass = []
    img = mpimg.imread(path,format='jpg')
    rclasses, rscores, rbboxes = process_image(img)
    height = img.shape[0]
    width = img.shape[1]
    for i in range(len(rclasses)):
        classname = dataClass[str(rclasses[i])]

        # Fetch information from wikipedia
        # information = wiki.summary(classname)
        information = wikiInfor[classname]

        ymin = int(rbboxes[i][0] * height)
        xmin = int(rbboxes[i][1] * width)
        ymax = int(rbboxes[i][2] * height)
        xmax = int(rbboxes[i][3] * width)
        location = [xmin, ymin, xmax - xmin, ymax - ymin]
        objectClass.append(
            {'className': classname, 'location': str(location), 'information': information, 'score': str(rscores[i])})

    # visualization.plt_bboxes(img, rclasses, rscores, rbboxes)
    return objectClass


# 用于判断文件后缀
def allowed_file(filename):
    return '.' in filename and filename.rsplit('.', 1)[1] in ALLOWED_EXTENSIONS


# 上传文件
@app.route('/api/upload', methods=['POST'], strict_slashes=False)
def api_upload():
    file_dir = os.path.join(basedir, app.config['UPLOAD_FOLDER'])
    if not os.path.exists(file_dir):
        os.makedirs(file_dir)
    f = request.files['myfile']  # 从表单的file字段获取文件，myfile为该表单的name值
    if f and allowed_file(f.filename):  # 判断是否是允许上传的文件类型
        fname = secure_filename(f.filename)
        print(fname)
        ext = fname.rsplit('.', 1)[1]  # 获取文件后缀
        unix_time = int(time.time())
        new_filename = str(unix_time) + '.' + ext  # 修改了上传的文件名
        f.save(os.path.join(file_dir, new_filename))  # 保存文件到upload目录
        # token = base64.b64encode(new_filename)
        token = new_filename
        print(token)

        results = detectFromimage(os.path.join(file_dir, new_filename))

        return jsonify(results)
    else:
        return jsonify({"errno": 1001, "errmsg": "File upload failed"})
