/*
Orb Engine — a structured knowledge and graph engine
Copyright (C) 2026  Subhradeep Sarkar

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.

*/

using Orb.Engine.Graph;
using Orb.Engine.Serialization;

namespace Orb.Engine.Storage;

public static class EntityStorage
{
    public static void Save(
        OrbEntity entity,
        string filePath)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var json = EntitySerializer.Serialize(entity);

        File.WriteAllText(filePath, json);
    }

    public static OrbEntity Load(
        string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "The entity file could not be found.",
                filePath);
        }

        var json = File.ReadAllText(filePath);

        return EntitySerializer.Deserialize(json);
    }
}