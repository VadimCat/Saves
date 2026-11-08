// Copyright (c) 2026 MauGames contributors. All rights reserved.
// Any use is prohibited without prior written permission from all four rights holders.
// License: LICENSE in the repository root.
// Reference: https://github.com/VadimCat/TapAway/blob/master/LICENSE

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Services.Saves
{
    public class QuaternionJsonConverter : JsonConverter<Quaternion>
    {
        public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("z");
            writer.WriteValue(value.z);
            writer.WriteEndObject();
        }

        public override Quaternion ReadJson(
            JsonReader reader,
            System.Type objectType,
            Quaternion existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            JObject obj = JObject.Load(reader);
            float x = obj["x"]?.Value<float>() ?? 0f;
            float y = obj["y"]?.Value<float>() ?? 0f;
            float z = obj["z"]?.Value<float>() ?? 0f;

            float wSquared = 1f - (x * x + y * y + z * z);
            float w = wSquared > 0f ? Mathf.Sqrt(wSquared) : 0f;

            return new Quaternion(x, y, z, w);
        }
    }
}
