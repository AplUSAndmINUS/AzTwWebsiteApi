

const handleCrudOperation = async <T>(
  operation: 'get' | 'set' | 'update' | 'delete',
  entityType: string,
  data?: T
) => {
  const service = getStorageService(entityType); // Dynamically select storage

  switch (operation) {
    case 'get':
      return service.getData();
    case 'set':
      return service.storeData(data);
    case 'update':
      return service.updateData(data);
    case 'delete':
      return service.deleteData();
    default:
      throw new Error('Invalid operation');
  }
};