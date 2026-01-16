using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MidiPlayerTK
{
    public class GameObjectUtilties
    {
        public static string BuildPathFromGameObject(GameObject go, string path = "")
        {
            path = "/" + go.name + path;
            if (go.transform.parent != null) path = BuildPathFromGameObject(go.transform.parent.gameObject, path);
            return path;
        }

        public static string BuildPathFromObject(Object o, string path = "")
        {
            if (o is GameObject) return BuildPathFromGameObject(o as GameObject, path);

            if (o is Component)
            {
                var cPath = BuildPathFromGameObject((o as Component).gameObject, path);
                return cPath + "/" + o.GetType().Name;
            }

            Debug.LogWarning(
                "ReferencedObject is not a GameObject nor a Component, so path cannot be built... will attempt to index by name and type instead");
            return o.name;
        }
    }


    public class ValueReference
    {
        public MemberInfo memberInfo;
        public MethodInfo methodInfo;
        public string pathToObject;
        public object referenceComponent;
        public object value;

        public ValueReference(MemberInfo memberInfo, object referenceObject, object value, string pathToObject)
        {
            this.memberInfo = memberInfo;
            referenceComponent = referenceObject;
            this.value = value;
            this.pathToObject = pathToObject;
        }

        public ValueReference(MethodInfo methodInfo, object referenceObject)
        {
            this.methodInfo = methodInfo;
            referenceComponent = referenceObject;
        }

        public void Resave()
        {
            SetValue(null, true, true);
        }

        public void SetValue(bool gameToEditor)
        {
            SetValue(value, gameToEditor);
        }

        private void SetValue(object value, bool gameToEditor, bool reset = false)
        {
            Debug.Log(string.Format("Setting... {0}.",
                (referenceComponent as Component).gameObject.name + "/" + referenceComponent.GetType().Name + "/" +
                pathToObject + memberInfo.Name));
            if (gameToEditor)
            {
                var path = GameObjectUtilties.BuildPathFromGameObject((referenceComponent as Component).gameObject);
                var go = GameObject.Find(path);
                if (go != null)
                {
                    referenceComponent = go.GetComponent(referenceComponent.GetType());
                }
                else
                {
                    Debug.LogWarning(string.Format("Cannot access GameObject at {0}, is it disabled?", path));
                    referenceComponent = null; //setting null to free old object.
                    return;
                }
            }

            var referenceObject = GetReference(referenceComponent, pathToObject);
            if (memberInfo is PropertyInfo)
            {
                var prop = memberInfo as PropertyInfo;
                if (value != null)
                {
                    #region HANDLE UNITY OBJECTS (property)

                    if (prop.PropertyType == typeof(GameObject))
                    {
                        var gPath = value as string;
                        if (gPath != null)
                        {
                            value = GameObject.Find(gPath);
                        }
                        else
                        {
                            Debug.LogWarning(string.Format("Cannot access GameObject at {0}, is it disabled?",
                                memberInfo.Name));
                            return;
                        }
                    }
                    else if (prop.PropertyType.IsSubclassOf(typeof(Component)) ||
                             prop.PropertyType == typeof(Component))
                    {
                        var cPath = value as string ?? "NO PATH";
                        var i = cPath.LastIndexOf('/');
                        if (i >= 0)
                        {
                            Debug.Log(cPath.Substring(0, i));
                            Debug.Log(cPath.Substring(i + 1));
                            var go = GameObject.Find(cPath.Substring(0, i));
                            if (go != null)
                            {
                                value = go.GetComponent(cPath.Substring(i + 1));
                            }
                            else
                            {
                                Debug.LogWarning(string.Format("Cannot access GameObject at {0}, is it disabled?",
                                    memberInfo.Name));
                                return;
                            }
                        }
                        else
                        {
                            Debug.LogWarning(string.Format("Could not find Component {0} at {1}", memberInfo.Name,
                                cPath));
                            return;
                        }
                    }
                    else if (prop.PropertyType.IsSubclassOf(typeof(Object)))
                    {
                        if (!(value is string))
                        {
                            value = null;
                        }
                        else
                        {
                            var objs = Resources.FindObjectsOfTypeAll(prop.PropertyType)
                                .Where(o => o.name == value as string);
                            if (objs.Count() == 1)
                            {
                                value = objs.First();
                            }
                            else if (objs.Count() == 0)
                            {
                                Debug.LogError("Property is Object but couldn't find it by type!");
                                Debug.Log("name: " + value);
                                Debug.Log("type: " + prop.PropertyType);
                                return;
                            }
                            else
                            {
                                Debug.LogError("Amibiguous Object!");
                                Debug.Log("name: " + value);
                                Debug.Log("type: " + prop.PropertyType);
                                return;
                            }
                        }
                    }

                    #endregion

                    if (value != null)
                    {
                        prop.SetValue(referenceObject, value, null);
                        Debug.Log(string.Format("Set as {0}", value));
                    }
                    else
                    {
                        prop.SetValue(referenceObject, null, null);
                        Debug.Log("Set as null");
                    }
                }
                else
                {
                    if (reset)
                    {
                        value = prop.GetValue(referenceObject, null);
                        Debug.Log(string.Format("Saved as {0}", value));
                    }
                    else
                    {
                        prop.SetValue(referenceObject, null, null);
                        Debug.Log("Set as null");
                    }
                }
            }
            else if (memberInfo is FieldInfo)
            {
                var field = memberInfo as FieldInfo;
                if (value != null)
                {
                    #region HANDLE UNITY OBJECTS (field)

                    if (field.FieldType == typeof(GameObject))
                    {
                        var gPath = value as string;
                        if (gPath != null)
                        {
                            value = GameObject.Find(gPath);
                        }
                        else
                        {
                            Debug.LogWarning(string.Format("Cannot access GameObject at {0}, is it disabled?",
                                memberInfo.Name));
                            return;
                        }
                    }
                    else if (field.FieldType.IsSubclassOf(typeof(Component)) || field.FieldType == typeof(Component))
                    {
                        var cPath = value as string;
                        var i = cPath.LastIndexOf('/');
                        if (i >= 0)
                        {
                            Debug.Log(cPath.Substring(0, i));
                            Debug.Log(cPath.Substring(i + 1));
                            var go = GameObject.Find(cPath.Substring(0, i));
                            if (go != null)
                            {
                                value = go.GetComponent(cPath.Substring(i + 1));
                            }
                            else
                            {
                                Debug.LogWarning(string.Format("Cannot access GameObject at {0}, is it disabled?",
                                    memberInfo.Name));
                                return;
                            }
                        }
                        else
                        {
                            Debug.LogWarning(string.Format("Could not find Component {0} at {1}", memberInfo.Name,
                                cPath));
                            return;
                        }
                    }
                    else if (field.FieldType.IsSubclassOf(typeof(Object)))
                    {
                        if (!(value is string))
                        {
                            value = null;
                        }
                        else
                        {
                            var objs = Resources.FindObjectsOfTypeAll(field.FieldType)
                                .Where(o => o.name == value as string);
                            if (objs.Count() == 1)
                            {
                                value = objs.First();
                            }
                            else if (objs.Count() == 0)
                            {
                                Debug.LogError("Field is Object but couldn't find it by type!");
                                Debug.Log("name: " + value);
                                Debug.Log("type: " + field.FieldType);
                                return;
                            }
                            else
                            {
                                Debug.LogError("Amibiguous Object!");
                                Debug.Log("name: " + value);
                                Debug.Log("type: " + field.FieldType);
                                return;
                            }
                        }
                    }

                    #endregion

                    if (value != null)
                    {
                        field.SetValue(referenceObject, value);
                        Debug.Log(string.Format("Set as {0}", value));
                    }
                    else
                    {
                        field.SetValue(referenceObject, null);
                        Debug.Log("Set as null");
                    }
                }
                else
                {
                    if (reset)
                    {
                        value = field.GetValue(referenceObject);
                        Debug.Log(string.Format("Saved as {0}", value));
                    }
                    else
                    {
                        field.SetValue(referenceObject, null);
                        Debug.Log("Set as null");
                    }
                }
            }
            else
            {
                Debug.LogError("No reference is attached to this ValueReference");
            }
        }

        private object GetReference(object o, string path)
        {
            var obj = o;
            var paths = path.Split(new[] { '/' });
            foreach (var p in paths)
                if (p.Length > 0)
                    obj = obj.GetType().GetField(p).GetValue(obj);
                else
                    break;
            return obj;
        }
    }
}