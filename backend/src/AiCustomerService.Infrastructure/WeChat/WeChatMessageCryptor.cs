using System.Security.Cryptography;
using System.Text;

namespace AiCustomerService.Infrastructure.WeChat;

/// <summary>
/// 微信公众号消息加解密工具类（完整实现）。
/// 算法：AES-256-CBC + PKCS#7，符合微信官方《消息加解密接入指引》。
/// EncodingAESKey 长度 43 位，Base64 解码后 32 字节。
/// </summary>
public class WeChatMessageCryptor
{
    private const int BlockSize = 32; // AES 块大小
    private readonly byte[] _aesKey;
    private readonly string _token;
    private readonly string _appId;

    public WeChatMessageCryptor(string token, string encodingAesKey, string appId)
    {
        if (string.IsNullOrEmpty(token)) throw new ArgumentException("token 不能为空");
        if (string.IsNullOrEmpty(encodingAesKey)) throw new ArgumentException("encodingAesKey 不能为空");
        if (encodingAesKey.Length != 43) throw new ArgumentException("encodingAesKey 长度必须为 43");
        if (string.IsNullOrEmpty(appId)) throw new ArgumentException("appId 不能为空");

        _token = token;
        _appId = appId;
        _aesKey = Convert.FromBase64String(encodingAesKey + "=");
    }

    /// <summary>
    /// 验证微信签名
    /// 签名规则：将 token、timestamp、nonce 三个参数进行字典序排序后拼接，SHA1 加密
    /// </summary>
    public bool VerifySignature(string signature, string timestamp, string nonce, string? encrypt = null)
    {
        var arr = new[] { _token, timestamp, nonce };
        if (!string.IsNullOrEmpty(encrypt)) arr = arr.Append(encrypt).ToArray();
        Array.Sort(arr);
        var joined = string.Join("", arr);
        var sha1 = SHA1.HashData(Encoding.UTF8.GetBytes(joined));
        var computed = BitConverter.ToString(sha1).Replace("-", "").ToLowerInvariant();
        return string.Equals(computed, signature, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 解密微信加密消息
    /// 密文结构（Base64 Decode 后）：
    ///   random(16B) + msg_len(4B, big-endian) + msg + receiveid(appId)
    /// </summary>
    public string Decrypt(string encryptedBase64, out string message)
    {
        message = string.Empty;
        if (string.IsNullOrEmpty(encryptedBase64))
            throw new ArgumentException("encryptedBase64 不能为空");

        byte[] cipherBytes;
        try { cipherBytes = Convert.FromBase64String(encryptedBase64); }
        catch (FormatException ex) { throw new CryptographicException("密文 Base64 解析失败", ex); }

        if (cipherBytes.Length < BlockSize + 4 + 1)
            throw new CryptographicException("密文长度不合法");

        // AES-256-CBC 解密：IV 取前 16 字节
        var iv = cipherBytes.Take(16).ToArray();
        var cipher = cipherBytes.Skip(16).ToArray();

        using var aes = Aes.Create();
        aes.Key = _aesKey;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        byte[] plain;
        try
        {
            using var decryptor = aes.CreateDecryptor();
            plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        }
        catch (CryptographicException ex)
        {
            throw new CryptographicException("AES 解密失败（EncodingAESKey 不正确？）", ex);
        }

        // 解析：random(16) + msg_len(4) + msg + receiveid
        // PKCS#7 padding 已被解密器去除
        if (plain.Length < 16 + 4 + 1)
            throw new CryptographicException("明文长度不合法");

        var contentLen = BitConverter.ToInt32(plain, 16);
        if (contentLen < 0 || contentLen > plain.Length - 20)
            throw new CryptographicException($"消息长度异常: {contentLen}");

        message = Encoding.UTF8.GetString(plain, 20, contentLen);

        // 校验 receiveid (appId)
        var receiveIdStart = 20 + contentLen;
        var receiveIdLen = plain.Length - receiveIdStart;
        if (receiveIdLen > 0)
        {
            var receiveId = Encoding.UTF8.GetString(plain, receiveIdStart, receiveIdLen);
            if (!string.Equals(receiveId, _appId, StringComparison.Ordinal))
                throw new CryptographicException($"receiveid 不匹配：expected={_appId}, actual={receiveId}");
        }

        return message;
    }

    /// <summary>
    /// 加密明文消息（用于被动回复时的加密模式）
    /// 输出结构：random(16B) + msg_len(4B) + msg + appId，Base64 编码
    /// </summary>
    public string Encrypt(string plain)
    {
        if (string.IsNullOrEmpty(plain)) throw new ArgumentException("plain 不能为空");

        var msgBytes = Encoding.UTF8.GetBytes(plain);
        var appIdBytes = Encoding.UTF8.GetBytes(_appId);

        var buffer = new byte[16 + 4 + msgBytes.Length + appIdBytes.Length];

        // 随机 16 字节
        RandomNumberGenerator.Fill(buffer.AsSpan(0, 16));
        // 消息长度（大端序）
        BitConverter.GetBytes(msgBytes.Length).CopyTo(buffer.AsSpan(16, 4));
        // 消息内容
        msgBytes.CopyTo(buffer.AsSpan(20));
        // appId
        appIdBytes.CopyTo(buffer.AsSpan(20 + msgBytes.Length));

        // AES 加密
        using var aes = Aes.Create();
        aes.Key = _aesKey;
        aes.IV = buffer.Take(16).ToArray();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var cipher = encryptor.TransformFinalBlock(buffer, 0, buffer.Length);
        return Convert.ToBase64String(cipher);
    }
}
