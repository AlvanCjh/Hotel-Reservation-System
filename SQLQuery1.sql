-- Check if Room R4 exists and has a TypeId
SELECT * FROM Rooms WHERE RoomId = 'R4';

-- Check if the TypeId exists in RoomTypes
SELECT * FROM RoomTypes WHERE TypeId = 'P01';

-- Check if any RoomTypes exist at all
SELECT COUNT(*) FROM RoomTypes;