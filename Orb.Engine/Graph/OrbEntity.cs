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



namespace Orb.Engine.Graph;

public sealed class OrbEntity
{
    // Stable identity
    public Guid Id { get; set; } = Guid.NewGuid();

    // Human-readable name
    public string? Name { get; set; }

    // Optional classification
    public string? Type { get; set; }

    // User-defined properties
    public Dictionary<string, OrbProperty> Properties { get; } = new();
}