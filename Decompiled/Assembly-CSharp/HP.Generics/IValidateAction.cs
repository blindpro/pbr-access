namespace HP.Generics;

public interface IValidateAction<T>
{
	void ValidateAction(T actionState);
}
