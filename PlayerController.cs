using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
  /******************Rigidbodyを用いたPlayerController*******************/


  /*接地判定*/
  bool isGrounded;
  RaycastHit hit;//RayCastが衝突したColliderを持つオブジェクトを格納する
  Vector3 rayPosition;//RayがPlayerオブジェクトに埋もれないために設定

  /*移動関係*/
  float inputHorizontal;//キー入力:横入力を-1〜1の間で受け取る
  float inputVertical;//キー入力:縦入力を-1〜1の間で受け取る
  public float moveSpeed = 10;//移動速度
  float jumpPower;//ジャンプ強度
  Vector3 gravity;//重力
  Vector3 inputMagnitude;//どれくらいの強度でキーを押したか
  Vector3 playerForward;//プレイヤにとって今どこが前かを決定する

  /*カメラ関係*/
  GameObject cam;//カメラをGameObjectとして取得
  Vector3 camForward;//カメラの前に設置したGameObjectの位置を取得する(?)

  Rigidbody rb;
  Animator anim;
  
  void Start()
  {
    rb = GetComponent<Rigidbody>();
    anim = GetComponent<Animator>();
    rayPosition = new Vector3(0, 1.0f, 0);
    gravity = new Vector3(0, rb.velocity.y, 0);
  }


  /*ゲームが開始したらStart()→FixedUpdate()→Update()→FixedUpdate()→Update()→
  の順でUpdate()より先にFixedUpdate()が呼ばれる*/
  void FixedUpdate()//接地判定に関する処理
  {
    /*Physics.SphereCast()：球体のレイを指定した方向へ飛ばし、コライダーの付いたオブジェクトがヒットするかを調べ、ヒットしたら詳細情報を返す
    Physics.SphereCast(Rayを飛ばす原点, 仮想球の半径, Rayを発射する向き, 当たった物の情報がhitに格納される, Rayを飛ばす最大距離)*/

    if (Physics.SphereCast(transform.position + rayPosition, transform.lossyScale.x * 0.5f, Vector3.down, out hit, 2f))//接地していた場合
    {
      isGrounded = true;
    }
    else//接地していなかった場合
    {
      isGrounded = false;
    }

    Debug.Log(isGrounded);
  }


  void Update()//移動に関する処理(+アニメーションに関する処理)
  {
    inputHorizontal = Input.GetAxis("Horizontal");//横軸(左右)の入力を取得する
    inputVertical = Input.GetAxis("Vertical");//縦軸(左右)の入力を取得する

    /*
    アニメーションに関する処理

    inputMagnitude = new Vector3(inputHorizontal, 0, inputVertical);で
    縦横左右の入力値が取得できる3次元ベクトルを用意しておく

    次にinputMagnitude.magnitudeでinputMagnitudeの"x + y + z"を足し合わせた
    値をfloat型で取得し、アニメーションで条件分岐をする際の値として使用する

    ※0.001という値は少なからず入力がある状態であることを示す
    */

    inputMagnitude = new Vector3(inputHorizontal, 0, inputVertical);//縦横の入力値が入った3次元ベクトル
    if (inputMagnitude.magnitude > 0.001)
    {
      anim.SetFloat("Speed", inputMagnitude.magnitude);
    }
    else//入力がなかった場合にinputMagnitudeに"0"を代入する(そうでないとinputMagnitudeに値が入ったままになってしまう)
    {
      anim.SetFloat("Speed", 0);
    }

    //カメラの正面を求める
    camForward = Vector3.Scale(Camera.main.transform.forward, new Vector3(1, 0, 1).normalized);

    /*****************************超重要①******************************
    移動に関する処理

    camForward * inputVertical + Camera.main.transform.right * inputHorizontalとは
    camForward * inputVertical：カメラから見て前方向 * 後方向への入力(前なら1、後ろなら-1)
    Camera.main.transform.right * inputHorizontal：カメラから見て右方向 * 左方向への入力(右なら1、左なら-1)を足した物である

    【疑問】
    なぜ2つのベクトルの値を足すのか？
    【理由】
    1.2つのベクトルの値を足すと、斜め方向への微妙な入力も算出出来るようになるから
    2.プレイヤーが向いている方向が3次元ベクトルの座標点で示せるから

    残る疑問：なぜCamera.main.transform.right * inputHorizontalの部分にはcamForwardと同様の処理が必要ないのか？
    *******************************************************************/

    playerForward = camForward * inputVertical + Camera.main.transform.right * inputHorizontal;//x,z方向へのあらゆるベクトルを前後左右の入力から計算する

    /*****************************超重要②******************************
       プレイヤーに力を加えて位置を動かす処理

       まず、プレイヤーにRigidbodyを用いて力を与える
       rb.velocity：物理演算の速度を変更する

       物体は『進む方向』 * 『進む速さ』によって移動する
       だから、playerForward(進む方向) * moveSpeed(進む速さ)を算出し
       rb.velocityに代入することで移動させる

       但し、これだけだとrb.velocity.y、すなわちyに加わる力(重力)が
       "0"になってしまっている(playerForwardのyの値は常に"0")
       なので、最後にgravityとして取得しておいた(0, rb.velocity.y, 0 )
       を足すことで重力を与える
       
       ※Rigidbodyを付けてるから重力が必ず付くと思いがち
       *******************************************************************/

    rb.velocity = playerForward * moveSpeed + new Vector3(0, rb.velocity.y, 0);


    /*****************************超重要③******************************
       移動方向に向きを変える処理

       Quaternion.LookRotation(Vector3 forward)で、オブジェクトの向きを
       変える事ができる

       だからtransform.rotationを、キーを押した方向を3次元ベクトルの値で
       表すplayerForwardのある点に書き換えれば、キーを押した方向を向く事ができる

       ※if (playerForward != Vector3.zero)がないと、入力がなかったフレーム
       の時にplayerForwardの中身が(0, 0, 0)になってしまうので
       入力がなくなると毎回(0, 0, 0)の方向に向き直ってしまう
       *******************************************************************/

    if (playerForward != Vector3.zero)
    {
      transform.rotation = Quaternion.LookRotation(playerForward);
    }
  }

  void OnDrawGizmos()
  {
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position + rayPosition, transform.lossyScale.x * 0.5f);
    Gizmos.DrawWireSphere(transform.position + rayPosition + Vector3.down * 2, transform.lossyScale.x * 0.5f);
  }
}
